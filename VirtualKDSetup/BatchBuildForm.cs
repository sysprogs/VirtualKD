using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Net;
using System.Text.RegularExpressions;

namespace VirtualKDSetup
{
    public partial class BatchBuildForm : Form
    {
        public BatchBuildForm()
        {
            InitializeComponent();

            textBox1.Text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\bin";
        }

        class BuildJob
        {
            public string FullVersion;
            public string SavedVersion;

            public override string ToString()
            {
                return FullVersion;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(textBox1.Text))
                Directory.CreateDirectory(textBox1.Text);
            WebClient clt = new WebClient();
            string data = clt.DownloadString(textBox2.Text);
            Regex r = new Regex("A HREF=\"([0-9]+)\\.([0-9]+)\\.([0-9]+)/\"");
            Dictionary<string, bool> added = new Dictionary<string, bool>();
            checkedListBox1.Items.Clear();
            foreach (Match match in r.Matches(data))
            {
                int major = int.Parse(match.Groups[1].ToString());
                int minor = int.Parse(match.Groups[2].ToString());
                if (major < 3)
                    continue;

                string fullver = string.Format("{0}.{1}.{2}", match.Groups[1], match.Groups[2], match.Groups[3]);
                string ver = string.Format("{0}.{1}.x", major, minor);
                if (!checkBox1.Checked)
                    ver = fullver;

                if (added.ContainsKey(ver))
                    continue;

                added[ver] = true;

                checkedListBox1.Items.Add(new BuildJob
                {
                    FullVersion = fullver,
                    SavedVersion = ver
                });
            }

            Application.DoEvents();

            for (int i = 0; i < checkedListBox1.Items.Count; i++ )
            {
                BuildJob job = checkedListBox1.Items[i] as BuildJob;
                if (Directory.Exists(textBox1.Text + "\\" + job.SavedVersion))
                {
                    checkedListBox1.SetItemChecked(i, true);
                    continue;
                }
                else
                {
                    checkedListBox1.SetItemChecked(i, true);
                    string dir = textBox1.Text + "\\" + job.SavedVersion;
                    Directory.CreateDirectory(dir);

                    VBoxBuildForm.FetchAndBuild(job.FullVersion, true, dir);
                }
            }
        }
    }
}
