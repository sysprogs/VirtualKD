using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Reflection;

namespace VirtualKDSetup
{
    public partial class VirtualBoxIntegrationForm : Form
    {
        VirtualBoxClient _Client = new VirtualBoxClient();

        public VirtualBoxIntegrationForm()
        {
            InitializeComponent();

            label2.Text = textBox1.Text = _Client.Version;
            if (_Client.Is64Bit)
            {
                label2.Text += " (64-bit)";
                comboBox1.SelectedIndex = 1;
            }
            else
            {
                label2.Text += " (32-bit)";
                comboBox1.SelectedIndex = 0;
            }

            label4.Text = _Client.VirtualBoxPath;
            lblLines.Text += string.Format("{0}\\.VirtualBox\\VirtualBox.xml", Environment.GetEnvironmentVariable("USERPROFILE"));

            textBox2.Text = string.Format("      <ExtraDataItem name=\"VBoxInternal/Devices/VirtualKD/0/Name\" value=\"Default\"/>\r\n" +
                "      <ExtraDataItem name=\"VBoxInternal/PDM/Devices/VirtualKD/Path\" value=\"{0}/{1}\"/>\n", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace('\\', '/'), _Client.Is64Bit ? "VBoxKD64.dll" : "VBoxKD.dll");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Visible = comboBox1.Visible = true;
            button1.Visible = label2.Visible = false;
            label1.Text = "Forced VirtualBox version:";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                label4.Text = Path.GetDirectoryName(openFileDialog1.FileName);
                label3.Text = "Forced VirtualBox path:";

                _Client = new VirtualBoxClient(label4.Text);
            }
        }

        string FileSourceToString(VirtualBoxClient.FileSource src)
        {
            switch(src)
            {
                case VirtualBoxClient.FileSource.Missing:
                    return "not found";
                case VirtualBoxClient.FileSource.VirtualBox:
                    return "original VirtualBox";
                case VirtualBoxClient.FileSource.VirtualKD:
                    return "patched VirtualKD";
                default:
                    return "unknown version";
            }
        }

        private void btnAuto_Click(object sender, EventArgs e)
        {
            if (_Client.VirtualBoxPath == null)
            {
                MessageBox.Show("Please select VirtualBox path", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            try
            {
                if (_Client.State == VirtualBoxClient.IntegrationState.Successful)
                {
                    MessageBox.Show("VirtualKD will now uninstall the old VBoxDD.DLL installed by pre-2.8 version.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    string dd = _Client.VirtualBoxPath + @"\VBoxDD.dll";
                    string dd0 = _Client.VirtualBoxPath + @"\VBoxDD0.dll";
                    string ddBak = _Client.VirtualBoxPath + @"\VBoxDD_OldVirtualKD.dll";

                    File.Move(dd, ddBak);
                    File.Move(dd0, dd);
                }

                string vboxKD = string.Format("{0}/{1}", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace('\\', '/'), _Client.Is64Bit ? "VBoxKD64.dll" : "VBoxKD.dll");
                NewVBoxClient.Install(vboxKD);
                MessageBox.Show("Installation successful.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = System.Windows.Forms.DialogResult.OK;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.SelectAll();
            textBox2.Copy();
        }

        private void lblLines_Click(object sender, EventArgs e)
        {

        }
    }
}
