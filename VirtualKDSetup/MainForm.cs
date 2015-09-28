using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace VirtualKDSetup
{
    public partial class MainForm : Form
    {
        string _GuestInstallerDir, _VMMonPath;

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool lpSystemInfo);

        public static bool Is64Bit()
        {
            try
            {
                if (IntPtr.Size == 8)
                    return true;
                bool retVal;
                IsWow64Process(Process.GetCurrentProcess().Handle, out retVal);
                return retVal;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public MainForm()
        {
            InitializeComponent();

            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\target";
                if (Directory.Exists(dir))
                    _GuestInstallerDir = dir;
                else
                    button6.Enabled = false;

                string vmmon = "vmmon.exe";
                if (Is64Bit())
                    vmmon = "vmmon64.exe";

                vmmon = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + vmmon;
                if (File.Exists(vmmon))
                    _VMMonPath = vmmon;
                else
                    button4.Enabled = false;

                VirtualBoxClient clt = new VirtualBoxClient();
                if ((clt.VirtualBoxPath == null) || (clt.VirtualBoxVersion == null))
                    label1.Text = "VirtualBox not found";
                else
                {
                    label1.Text = "VirtualBox " + clt.Version + " detected. ";
                    switch(clt.State)
                    {
                        case VirtualBoxClient.IntegrationState.NotInstalled:
                            {
                                string fn = NewVBoxClient.GetRegisteredDLLPath();
                                if (fn == null)
                                    label1.Text += "VirtualKD is not integrated. ";
                                else
                                    label1.Text += "No problems found. ";
                            }
                            break;
                        case VirtualBoxClient.IntegrationState.Successful:
                            label1.Text += "Pre-2.8 VirtualKD is installed.";
                            break;
                        case VirtualBoxClient.IntegrationState.VBoxReinstallRequired:
                            label1.Text += "VirtualBox must be reinstalled! ";
                            button1.Enabled = false;
                            break;
                    }
                    if (clt.StateComment != null)
                        label1.Text += (clt.StateComment + ".");
                }
            }
            catch (System.Exception)
            {
            }

        }

        void OpenURL(string URL)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = URL;
            p.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenURL("http://virtualkd.sysprogs.org/");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenURL(_VMMonPath);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenURL("http://virtualkd.sysprogs.org/");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            OpenURL(_GuestInstallerDir);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            VirtualBoxIntegrationForm frm = new VirtualBoxIntegrationForm();
            frm.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
