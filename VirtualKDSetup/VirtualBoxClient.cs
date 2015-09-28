using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;

namespace VirtualKDSetup
{
    class VirtualBoxClient
    {
        public readonly string VirtualBoxPath;
        public readonly int[] VirtualBoxVersion;
        public readonly bool Is64Bit;

        public string Version
        {
            get
            {
                if (VirtualBoxVersion == null)
                    return null;
                List<string> lst = new List<string>();
                foreach (var v in VirtualBoxVersion)
                    lst.Add(v.ToString());
                return string.Join(".", lst.ToArray());
            }
        }

        public VirtualBoxClient()
            :this (null)
        {
        }

        public VirtualBoxClient(string dir)
        {
            try
            {
                Is64Bit = MainForm.Is64Bit();
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Oracle\VirtualBox");
                if (dir == null)
                {
                    if (key != null)
                        dir = key.GetValue("InstallDir") as string;
                    else
                        dir = Environment.GetEnvironmentVariable(Is64Bit ? "ProgramW6432" : "ProgramFiles") + "\\Oracle\\VirtualBox";
                    if (!Directory.Exists(dir))
                        return;
                }
                VirtualBoxPath = dir.TrimEnd('\\');

                if (key != null)
                {
                    string[] ver = ((string)key.GetValue("VersionExt")).Split('.');
                    VirtualBoxVersion = new int[ver.Length];
                    for (int i = 0; i < VirtualBoxVersion.Length; i++)
                        VirtualBoxVersion[i] = int.Parse(ver[i]);
                }

                DetectFileTypes();
                DetectState();
            }
            catch (System.Exception)
            {
            	
            }
        }

        public enum IntegrationState
        {
            VBoxReinstallRequired,
            Unknown,
            NotInstalled,
            Successful,
        }

        public enum FileSource
        {
            Missing,
            VirtualKD,
            VirtualBox,
            Unknown,
        }

        FileSource _VBoxDD, _VBoxDD0;
        public VirtualKDSetup.VirtualBoxClient.FileSource VBoxDD0
        {
            get { return _VBoxDD0; }
        }
        public VirtualKDSetup.VirtualBoxClient.FileSource VBoxDD
        {
            get { return _VBoxDD; }
        }
        
        IntegrationState _State;
        
        public VirtualKDSetup.VirtualBoxClient.IntegrationState State
        {
            get { return _State; }
        }

        string _StateComment;

        public string StateComment
        {
            get { return _StateComment; }
        }

        const int FileSizeThreshold = 512 * 1024;

        bool find(byte[] data, byte[] seq)
        {
            byte v = seq[0];
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != v)
                    continue;
                bool match = true;
                for (int j = 0; j < seq.Length; j++)
                    if (data[i + j] != seq[j])
                    {
                        match = false;
                        break;
                    }

                if (match)
                    return true;
            }
            return false;
        }

        void DetectFileTypes()
        {
            string fn = VirtualBoxPath + @"\VBoxDD.dll";
            if (!File.Exists(fn))
                _VBoxDD = FileSource.Missing;
            else
            {
                try
                {
                    using (FileStream fs = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        bool sizeMatch = (fs.Length < FileSizeThreshold);
                        if (fs.Length > 50 * 1024 * 1024)
                            _VBoxDD = FileSource.Unknown;
                        else
                        {
                            byte[] data = new byte[fs.Length];
                            if (fs.Read(data, 0, (int)fs.Length) != (int)fs.Length)
                                _VBoxDD = FileSource.Unknown;
                            else
                            {
                                bool foundSig1 = find(data, Encoding.ASCII.GetBytes("~kdVMvA"));
                                bool foundSig2 = find(data, Encoding.ASCII.GetBytes("++kdVMvA"));

                                if ((foundSig1 != foundSig2) || (foundSig1 != sizeMatch))
                                    _VBoxDD = FileSource.Unknown;
                                else
                                    _VBoxDD = sizeMatch ? FileSource.VirtualKD : FileSource.VirtualBox;
                            }

                        }
                    }

                }
                catch (System.Exception)
                {
                    _VBoxDD = FileSource.Unknown;
                }
            }

            fn = VirtualBoxPath + @"\VBoxDD0.dll";
            if (!File.Exists(fn))
                _VBoxDD0 = FileSource.Missing;
            else
            {
                try
                {
                    using (FileStream fs = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        bool sizeMatch = (fs.Length < FileSizeThreshold);
                        _VBoxDD0 = sizeMatch ? FileSource.Unknown : FileSource.VirtualBox;
                    }

                }
                catch (System.Exception)
                {
                    _VBoxDD0 = FileSource.Unknown;
                }
            }
        }
    
        void DetectState()
        {
            _State = IntegrationState.Unknown;
            _StateComment = null;

            switch(_VBoxDD)
            {
                case FileSource.Missing:
                    if (_VBoxDD0 == FileSource.VirtualBox)
                    {
                        _State = IntegrationState.Unknown;
                        _StateComment = "You have deleted VBoxDD.dll, but VBoxDD0.dll seems OK.";
                    }
                    else
                    {
                        _State = IntegrationState.VBoxReinstallRequired;
                        _StateComment = "Missing VBoxDD.dll";
                    }
                    break;
                case FileSource.VirtualBox:
                    _State = IntegrationState.NotInstalled;
                    _StateComment = null;
                    break;
                case FileSource.VirtualKD:
                    switch(_VBoxDD0)
                    {
                        case FileSource.Missing:
                            _State = IntegrationState.VBoxReinstallRequired;
                            _StateComment = "Missing VBoxDD0.dll";
                            break;
                        case FileSource.Unknown:
                        case FileSource.VirtualKD:
                            _State = IntegrationState.VBoxReinstallRequired;
                            _StateComment = "Unrecognized VBoxDD0.dll";
                            break;
                        case FileSource.VirtualBox:
                            _State = IntegrationState.Successful;
                            _StateComment = null;
                            break;
                    }
                    break;
                case FileSource.Unknown:
                    _State = IntegrationState.Unknown;
                    _StateComment = "Unrecognized VBoxDD.dll";
                    break;
            }
        }
    
    }
}
