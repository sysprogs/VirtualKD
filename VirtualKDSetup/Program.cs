using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace VirtualKDSetup
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (Environment.CommandLine.Contains("/BatchMode"))
                Application.Run(new BatchBuildForm());
            else
                Application.Run(new MainForm());
        }
    }
}
