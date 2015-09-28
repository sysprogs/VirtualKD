using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Tar;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.BZip2;

namespace VirtualKDSetup
{
    public partial class DownloadProgressForm : Form
    {
        string m_URL, m_Dir;
        string m_FileName;
        string m_NamePrefixToDrop;

        public delegate bool UnpackFilter(string fn);

        UnpackFilter _Filter;

        DownloadProgressForm(string URL, string Dir, string NamePrefixToDrop)
        {
            InitializeComponent();
            m_URL = URL;
            m_Dir = Dir;
            m_NamePrefixToDrop = NamePrefixToDrop;
            m_FileName = m_URL.Substring(m_URL.LastIndexOf('/') + 1);
            label1.Text = "Downloading " + m_FileName + "...";

            if (m_Dir == null)
                label2.Visible = progressBar2.Visible = false;
            else
                label2.Text = "Unpacking to " + m_Dir + "...";
        }

        public static DialogResult DownloadAndUnpack(string URL, string Dir, string Title, string NamePrefixToDrop, UnpackFilter filter)
        {
            try
            {
                DownloadProgressForm frm = new DownloadProgressForm(URL, Dir, NamePrefixToDrop);
                frm._Filter = filter;
                frm.Text = Title;
                return frm.ShowDialog();
            }
            catch (System.Exception)
            {
                return DialogResult.Cancel;
            }
        }

        byte[] DownloadedData;

        public static byte[] DownloadToArray(string URL, string Title, string NamePrefixToDrop)
        {
            try
            {
                DownloadProgressForm frm = new DownloadProgressForm(URL, null, NamePrefixToDrop);
                frm.Text = Title;
                if (frm.ShowDialog() == DialogResult.Cancel)
                    return null;
                return frm.DownloadedData;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private void DownloadProgressForm_Load(object sender, EventArgs e)
        {
            WebClient clt = new WebClient();
            clt.DownloadDataCompleted += new DownloadDataCompletedEventHandler(clt_DownloadDataCompleted);
            clt.DownloadProgressChanged += new DownloadProgressChangedEventHandler(clt_DownloadProgressChanged);
            clt.DownloadDataAsync(new Uri(m_URL));
        }

        void clt_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.TotalBytesToReceive == 0)
                return;
            int val = (int)((e.BytesReceived * progressBar1.Maximum) / e.TotalBytesToReceive);
            if (val > progressBar1.Maximum)
                val = progressBar1.Maximum;
            progressBar1.Value = val;            
        }

        void clt_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (m_Dir == null)
            {
                DownloadedData = e.Result;
                DialogResult = DialogResult.OK;
                return;
            }

            if (e.Error != null)
            {
                DialogResult = DialogResult.No;
                return;
            }

            string file = m_FileName;
            Stream strm = new MemoryStream(e.Result);
            for (; ; )
            {
                string ext = Path.GetExtension(file);
                if (ext == ".zip")
                {
                    UnpackZip(strm);
                    break;
                }
                else if (ext == ".tar")
                {
                    UnpackTar(strm);
                    break;
                }
                else if (ext == ".gz")
                    strm = UnpackGzip(strm);
                else if (ext == ".bz2")
                    strm = new BZip2InputStream(strm);
                else
                    throw new Exception("Unknown archive extension: " + Path.GetExtension(file));
                file = file.Substring(0, file.Length - ext.Length);
            }
            strm.Dispose();
            DialogResult = DialogResult.OK;
        }

        Stream UnpackGzip(Stream strm)
        {
            return new GZipStream(strm, CompressionMode.Decompress);
        }


        void UnpackZip(Stream strm)
        {
            ZipFile file = new ZipFile(strm);
            byte[] tempBuf = new byte[65536];

            progressBar2.Maximum = (int)file.Count;

            if (!Directory.Exists(m_Dir))
                Directory.CreateDirectory(m_Dir);
            int idx = 0;

            foreach (ZipEntry entry in file)
            {
                string strName = entry.Name;
                if (m_NamePrefixToDrop != "")
                    {
                    int sepidx = strName.IndexOf('/');
                    if (sepidx != -1)
                    {
                        string firstComponent = strName.Substring(0, sepidx);
                        if (firstComponent.ToUpper() == m_NamePrefixToDrop.ToUpper())
                            strName = strName.Substring(m_NamePrefixToDrop.Length + 1);
                    }
                }

                if (strName == "")
                    continue;

                string fn = m_Dir + "\\" + strName;

                if (entry.IsDirectory)
                {
                    if (!Directory.Exists(fn))
                        Directory.CreateDirectory(fn);
                }
                else
                {
                    Stream istrm = file.GetInputStream(entry);
                    FileStream ostrm = new FileStream(fn, FileMode.Create);
                    for (; ; )
                    {
                        int done = istrm.Read(tempBuf, 0, tempBuf.Length);
                        if (done == 0)
                            break;
                        ostrm.Write(tempBuf, 0, done);
                    }
                    istrm.Dispose();
                    ostrm.Dispose();
                }
                if (++idx > progressBar2.Maximum)
                    idx = progressBar2.Maximum;
                progressBar2.Value = idx;
                Application.DoEvents();
            }
        }

        void UnpackTar(Stream strm)
        {
            TarInputStream tar = new TarInputStream(strm);

            byte[] tempBuf = new byte[65536];
            progressBar2.Style = ProgressBarStyle.Marquee;
            int done = 0;
            for (; ; )
            {
                TarEntry entry = tar.GetNextEntry();
                if (entry == null)
                    break;

                string strName = entry.Name;
                string firstComponent = strName.Substring(0, strName.IndexOf('/'));
                if (firstComponent.ToUpper() == m_NamePrefixToDrop.ToUpper())
                    strName = strName.Substring(m_NamePrefixToDrop.Length + 1);

                if (strName == "")
                    continue;

                string fn = m_Dir + "\\" + strName;

                if ((_Filter == null) || (_Filter(strName)))
                    if (entry.IsDirectory)
                    {
                        if (!Directory.Exists(fn))
                            Directory.CreateDirectory(fn);
                    }
                    else
                        using (FileStream ostrm = new FileStream(fn, FileMode.Create))
                            tar.CopyEntryContents(ostrm);
                if ((done += (int)entry.Size) > progressBar2.Maximum)
                    done = progressBar2.Maximum;
                progressBar2.Value = done;
                Application.DoEvents();
            }
        }

    }
}
