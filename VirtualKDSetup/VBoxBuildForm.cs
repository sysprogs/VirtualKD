using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;

namespace VirtualKDSetup
{
    public partial class VBoxBuildForm : Form
    {
        bool _X64;
        string _DirForAutoMode;

        VBoxBuildForm(string vboxVer, bool x64, string dirForAutoMode)
        {
            InitializeComponent();

            _X64 = x64;
            textBox3.Text = Environment.GetEnvironmentVariable("TEMP") + @"\VirtualKDBuild";

            //for (int ver = 10; ver >= 8; ver--)
            int ver = 9;
            {
                string vs = ReadVSRootFromRegistry(ver);
                if (vs != null)
                {
                    textBox2.Text = vs;
                    //break;
                }
            }

            textBox1.Text = string.Format("http://download.virtualbox.org/virtualbox/{0}/VirtualBox-{0}.tar.bz2", vboxVer);
            _DirForAutoMode = dirForAutoMode;

            if (_DirForAutoMode != null)
            {
                timer1.Enabled = true;
                button1.Enabled = false;
                textBox3.Text = Environment.GetEnvironmentVariable("TEMP") + @"\VirtualKDBuild_" + vboxVer;
            }
        }

        public static byte[] FetchAndBuild(string vboxVer, bool x64)
        {
            return FetchAndBuild(vboxVer, x64, null);
        }

        public static byte[] FetchAndBuild(string vboxVer, bool x64, string dirForAutoMode)
        {
            VBoxBuildForm frm = new VBoxBuildForm(vboxVer, x64, dirForAutoMode);
            if (frm.ShowDialog() != DialogResult.OK)
                return null;
            return frm._FileContents;
        }

        static internal string ReadVSRootFromRegistry(int MajorVersion)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format(@"SOFTWARE\{1}Microsoft\VisualStudio\{0}.0", MajorVersion, (IntPtr.Size == 8) ? "Wow6432Node\\" : ""));
            
            if (key == null)
                return null;
            string regPath = key.GetValue("InstallDir") as string;
            if (regPath == null)
                return null;
            regPath = regPath.TrimEnd('\\');
            if (regPath.EndsWith(@"\Common7\IDE"))
                regPath = regPath.Substring(0, regPath.Length - 12);
            if (!File.Exists(regPath + @"\Common7\IDE\devenv.com"))
                return null;
            return regPath + @"\Common7\IDE\devenv.com";
        }

        bool IsVBoxIncludeFile(string fn)
        {
            int idx = fn.IndexOf('/');
            if (idx == -1)
                return true;    //First-level dir

            if (fn.Substring(idx + 1).ToLower().StartsWith("include/"))
                return true;
            return false;
        }

        string _SLNFile, _TargetFile;

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                button1.Enabled = false;
                textBox4.Text = "";
                string dir = textBox3.Text;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string vboxDir = dir + @"\VBox";
                if (!Directory.Exists(vboxDir))
                    Directory.CreateDirectory(vboxDir);
                else if (checkBox1.Checked)
                {
                    MessageBox.Show("Directory " + vboxDir + " already exists. It won't be deleted after successful build.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    checkBox1.Checked = false;
                }

                switch (DownloadProgressForm.DownloadAndUnpack(textBox1.Text, vboxDir, "Downloading VirtualBox OSE...", "", IsVBoxIncludeFile))
                {
                    case DialogResult.OK:
                        break;
                    case DialogResult.No:
                        if (DownloadProgressForm.DownloadAndUnpack(textBox1.Text.Replace(".tar.bz2", "-OSE.tar.bz2"), vboxDir, "Downloading VirtualBox OSE...", "", IsVBoxIncludeFile) != DialogResult.OK)
                            return;
                        break;
                    default:
                        return;
                }

                string[] dirs = Directory.GetDirectories(vboxDir, "VirtualBox-*");
                if (dirs.Length != 1)
                {
                    MessageBox.Show("Directory " + vboxDir + " should only have 1 VirtualBox subdirectory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string newDir = Path.GetDirectoryName(dirs[0]) + @"\main";
                if (Directory.Exists(newDir))
                    Directory.Delete(newDir, true);

                Directory.Move(dirs[0], newDir);

                WebClient clt = new WebClient();
                string ver = clt.DownloadString("http://virtualkd.sysprogs.org/vbox/autobox.ver");
                string URL = string.Format("http://virtualkd.sysprogs.org/vbox/autobox-{0}.zip", ver);
                if (DownloadProgressForm.DownloadAndUnpack(URL, dir, "Downloading VBoxDD.dll sources...", "", null) != DialogResult.OK)
                    return;

                _SLNFile = dir + @"\VBoxDD\VBoxDD.sln";

                if (_DirForAutoMode != null)
                    _X64 = false;
                LaunchBuild();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button1.Enabled = true;
            }
        }

        void LaunchBuild()
        {
            if (_X64)
                _TargetFile = textBox3.Text + @"\VBoxDD\x64\Release\VBoxDD.dll";
            else
                _TargetFile = textBox3.Text + @"\VBoxDD\Release\VBoxDD.dll";

            Process proc = new Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(_SLNFile);
            proc.StartInfo.FileName = textBox2.Text;
            proc.StartInfo.Arguments = string.Format("{0} /Build Release|{1}", Path.GetFileName(_SLNFile), _X64 ? "x64" : "Win32");
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.EnableRaisingEvents = true;
            proc.OutputDataReceived += new DataReceivedEventHandler(proc_OutputDataReceived);
            proc.Exited += new EventHandler(proc_Exited);
            if (!proc.Start())
            {
                proc_Exited(null, null);
            }
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
        }

        byte[] _FileContents;

        void proc_Exited(object sender, EventArgs e)
        {
            BeginInvoke(new ThreadStart(delegate 
                {
                    button1.Enabled = true;

                    if (!File.Exists(_TargetFile))
                    {
                        if (MessageBox.Show("Looks like the build has failed. Do you want to open the solution file in Visual Studio?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            try
                            {
                                Process proc = new Process();
                                proc.StartInfo.FileName = _SLNFile;
                                proc.Start();
                            }
                            catch (System.Exception ex)
                            {
                                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        return;
                    }

                    try
                    {
                        using (FileStream fs = new FileStream(_TargetFile, FileMode.Open))
                        {
                            var contents = new byte[fs.Length];
                            if (fs.Read(contents, 0, contents.Length) == contents.Length)
                                _FileContents = contents;
                        }

                        if (_FileContents != null)
                        {
                            if (_DirForAutoMode != null)
                            {
                                string targetDir = _DirForAutoMode + "\\" + (_X64 ? "x64" : "x86");
                                if (!Directory.Exists(targetDir))
                                    Directory.CreateDirectory(targetDir);
                                File.Copy(_TargetFile, targetDir + @"\VBoxDD.dll");

                                if (_X64 == false)
                                {
                                    _X64 = true;
                                    LaunchBuild();
                                    return;
                                }
                            }

                            if (checkBox1.Checked)
                                Directory.Delete(textBox3.Text, true);
                            DialogResult = DialogResult.OK;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }));
        }

        void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            BeginInvoke(new ThreadStart(delegate 
                {
                    textBox4.Text += e.Data + "\r\n";
                    textBox4.SelectionStart = textBox4.Text.Length;
                    textBox4.ScrollToCaret();
                }));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            button1_Click(null, null);
        }

    }
}
