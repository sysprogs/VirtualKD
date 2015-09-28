using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualKDSetup
{
    class NewVBoxClient
    {
        static string DoGetRegisteredDLLPath()
        {
            VirtualBox.VirtualBox vbox = new VirtualBox.VirtualBox();
            if (vbox.GetExtraData("VBoxInternal/Devices/VirtualKD/0/Name") == null)
                return null;
            return vbox.GetExtraData("VBoxInternal/PDM/Devices/VirtualKD/Path");
        }

        static public string GetRegisteredDLLPath()
        {
            try
            {
                return DoGetRegisteredDLLPath();
            }
            catch
            {
                return null;
            }
        }

        public static void Install(string vboxKD)
        {
            VirtualBox.VirtualBox vbox = new VirtualBox.VirtualBox();
            vbox.SetExtraData("VBoxInternal/Devices/VirtualKD/0/Name", "Default");
            vbox.SetExtraData("VBoxInternal/PDM/Devices/VirtualKD/Path", vboxKD);
        }
    }
}
