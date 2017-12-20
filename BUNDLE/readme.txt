VirtualKD - Kernel Debugger extension for VMWare and VirtualBox
Version 2.8
http://virtualkd.sysprogs.org/

Copyright (c) Ivan Shcherbakov, 2009-2012 [ivan@sysprogs.org]

-===============QUICKSTART===============-
	1. Run VirtualKDSetup.exe (renamed to VirtualBoxIntegration.exe from v3.0+) on host (only needed if VirtualBox is used)
	2. Run target\vminstall.exe on every virtual machine.
	3. Run vmmon.exe (or vmmon64.exe) to start.
	
If you have problems with VirtualBox integration, re-register VBoxKD64.dll/VBoxKD.dll by running (non-elevated mode!)
	regsvr32 VBoxKD64.dll
	
Ensure that VirtualBox is closed and no VBoxSVC service is running in the background.
	
-========================================-

OVERVIEW
VirtualKD allows debugging your Windows kernel running on VirtualBox and VMWare using very fast custom transport.
It seamlessly integrates with WinDBG and VisualDDK providing a much faster debugging experience.

COMPATIBILITY
VirtualKD supports both x86 and x64 guest operating systems and was tested with the following OSes:
	* Windows Vista 32bit
	* Windows XP 32bit
	* Windows XP 64bit
	* Windows 7 32bit
	* Windows 7 64bit
	* Windows 8 64bit
VMWare: all modern versions are supported. The following versions were tested:
	* VMWare Server 1.0.5
	* VMWare Server 2.0.0
	* VMWare Workstation 6.5.1
	* VMWare Workstation 8.0.4

VirtualKD supports 64-bit host and 64-bit versions of VMWare.

INSTALLATION

When you normally debug Windows kernel inside a VM (without VirtualKD), a virtual COM port is created and mapped to a named pipe. 
Windows kernel interacts with the COM port via the KDCOM.DLL library and WinDbg connects to it using a named pipe
specified in VirtualBox settings.

VirtualKD provides an override to this scheme:
1. The KDBAZIS.DLL debug transport is used instead of KDCOM.DLL. KDBAZIS.DLL directly communicates with VirtualBox/VMWare in a fast way.
2. KDCLIENT.DLL loaded into VirtualBox/VMWare creates a named pipe similar to the virtual COM port one. However, it's much faster then the COM port.

Installing VirtualKD requires installing both guest part (KDBAZIS.DLL) and host part (KDCLIENT64.DLL):

If you are using VMWare, not special host-part installation is required. Just launch VMMON64.EXE or VMMON.EXE once the guest part is installed.
If you are using VirtualBox, you need to register VBoxKD.DLL within VirtualBox by running the following command:
	regsvr32 VBoxKD64.DLL (or VBoxKD.DLL for 32-bit VirtualBox).
	
If this does not work, open the C:\Users\<USERNAME>\.VirtualBox\VirtualBox.xml file and manually insert the following lines inside <ExtraData>:
      <ExtraDataItem name="VBoxInternal/Devices/VirtualKD/0/Name" value="Default"/>
      <ExtraDataItem name="VBoxInternal/PDM/Devices/VirtualKD/Path" value="(path to VBoxKD or VBoxKD64 with '/' instead of '\')/VBoxKD64.dll"/>

Note that you might need to close all instances of VirtualBox and VBoxSVC before doing it, otherwise VirtualBox.xml will be rolled back.	  
	  
Installing guest part is simple: just copy target\vminstall.exe to your VM and run it from there. If anything goes wrong, you can try installing it manually:

A) Dynamic patching (not recommended).
Copy both KDPATCH.SYS and KDBAZIS.DLL to SYSTEM32\DRIVERS directory of your virtual machine
(yes, both DLL and SYS in the same place) and apply the KDPATCH.REG file (or manually create a legacy driver 
running KDPATCH.SYS). Then, perform the following sequence:
1) Start the virtual machine
2.VMWare) BEFORE OS starts to load, patch the VMWare process on host side using VMXPATCH.EXE or VMMON.EXE (see below)
2.VirtualBox) Rename VBoxDD.dll in VirtualBox directory to VBoxDD0.dll. Copy VBoxDD.DLL from VirtualKD instead of original VBoxDD.dll
3) Start your virtual Windows in a normal debug mode with a virtual COM port
4) Ensure that WinDBG establishes connection (our driver is not involved now)
5) Let the OS boot
6) Wait for the KDPATCH.SYS driver to load (if you set it to manual mode, run
	"net start kdpatch"). This will redirect all debugging activity from the COM
	port simulated by VMWare to our fast debugging interface (VMWare needs to be
	already patched at this moment).
7) Start the WinDBG or KD debugger. The pipe name for the fast debugging is the following:
	\\.\pipe\kd_<dirname>, when your VM is located at X:\something\<dirname>\filename.vmx
8) You can close the first instance of WinDBG.
Alternatively, you can stop the KDPATCH.SYS driver at any moment to direct debug activity
back to the VMWare COM port.

This method is useful if you want to play with the VirtualKD sources and to load different
versions of KDBAZIS.DLL without rebooting the virtual machine. Just start the patcher
service, and the KDBAZIS.DLL is used instead of KDCOM.DLL. Stop it and KDCOM.DLL gains
control back.

B) Static patching (recommended)
1) Copy KDBAZIS.DLL to SYSTEM32 directory of your guest OS. There should already exist
KDCOM.DLL and KD1394.DLL files.
--- For XP ---
2) Open your boot.ini file. If you are using the COM debugging, you should have a line like this:
multi(0)disk(0)rdisk(0)partition(1)\WINDOWS="Microsoft Windows XP Professional" /noexecute=optin /fastdetect /DEBUG /DEBUGPORT=COM1
3) Change it to this:
multi(0)disk(0)rdisk(0)partition(1)\WINDOWS="Microsoft Windows XP Professional" /noexecute=optin /fastdetect /DEBUG /DEBUGPORT=VM
--- For Vista/Win7 ---
2,3) Run the following command line:
	bcdedit /set dbgtransport kdbazis.dll
---
4) Reboot your virtual machine and wait for the OS selection dialog.
5.VMWare) Patch VMWare executable using VMXPATCH.EXE or VMMON.EXE (see below).
6) Start WinDBG or KD. Use \\.\pipe\kd_<dirname> as pipe name (see above).
7) Start your guest OS

The /DEBUGPORT=VM parameter in boot.ini instructs Windows to use the KDBAZIS.DLL file as 
the packet layer driver for kernel debugging. This is more flexible than patching KDCOM.DLL,
as it does not require booting in COM debug mode.
In Vista/Win7 this functionality is achieved by setting the "dbgtransport option via BCDEDIT.
 
TWEAKING
You can modify some parameters in registry under SOFTWARE\BazisSoft\VirtualKD\Patcher
1) AllowPatchingAtTableStart. Set it to 0 if your VMWare crashes when being patched.
2) AllowReplacingFirstCommand. Set it to 1 if patching fails (and debug output indicates
	something like "0 free entries").
3) DefaultPatchingAtTableStart. You can try setting this to increase the performance
    (just a bit), but in can make VMWare crash on patching. Feel free to try ;)
Additionally, you can set the WaitForOS to 0 in VirtualKD\Monitor to let the debugger
be started immediately when a VM is detected (without waiting for OS to load).

SOURCE STRUCTURE
The information about source code structure and some hints for experimenting will be soon 
published at http://VirtualKD.sysprogs.org/.

CHANGE LOG
v1.0
	Initial version
	
v1.1
	Fixed handler loss after Virtual Machine reset
	Fixed bug with VMWare hanging when no debugger is connected
	Added patcher/packet level log displaying in VMMON
	Added support for KDCLIENT.DLL unloading from VMMON
	Added advanced statistics reporting to VMMON
	Added permissive SECURITY_ATTRIBUTES to statistics-related objects to support non-admin VMWare instances
	Added debugger command line customization
	Added proxy mode support for debug VMMON builds
	Added TraceAssist feature
	Implemented buffered VMWare GuestRPC resulting in ~1.7x communication speedup
	
v1.2 (Only host-side part changed)
	Fixed rare bug, when disconnecting debugger in the middle of a KdSendPacket() call caused hanging
	Reduced CPU usage from 100% to 0% when a VM is active and no debugger is connected
	Added workaround for truncated Driver Verifier messages
	Added on-demand packet logging feature for easy packet-level KD protocol analyzis
	Added API for detecting & patching VMs to KDCLIENT.DLL to support VisualDDK integration
	Pipe name is now generated based on VMWARE-VMX.EXE command line, instead of current directory (fix for rare "kd_" pipes)
	Added support for sending generated 'Target OS Shutdown' packet when VM is stopping to force debugger to disconnect
	Added ",reconnect" option to WinDbg command line, instructing it to reconnect a pipe, when it is closed
	(Due to 2 previous features, debugger does not need to be restarted when a VM is restored from a snapshot, while OS is running)
	
v1.3 (Only host-side part changed)
	Added support for 64-bit host operating systems and 64-bit versions of VMWare.
	
v2.0 (KDVMWare was renamed to VirtualKD)
	Added support for VirtualBox
	
v2.1 Added support for Windows 7 host and guest machines
	 Added test signature to KDVM.DLL allowing it to run on Vista x64 in TESTSIGNING mode
	 Added automatic guest machine installer
		
v2.2 Improved integration with VisualDDK
	 Added support for VMWARE-VMX-DEBUG.EXE etc.

v2.3 Fixed memory leak in VMMON
	 Added support for patching VMs from non-administrator accounts via IPC with VMMON
	 Fixed bug when only first running VM was reported	 	 
	 Added support for VirtualBox 3.1.x
	 
v2.4 Fixed compatibility with UAC (.vmpatch files are now saved to Application Data)

v2.5 Added "instant break" feature, reducing debugger break-in time to zero when using VisualDDK
	 Added support for restoring VM (VirtualBox/VMWare) to last snapshot from VMMON/VisualDDK
	 
v2.5.1 Added support for VirtualBox 3.2.x
v2.5.2 Added support for VirtualBox 4.x
v2.5.3 Fixed ICH9 compatibility issue
v2.6 Renamed KDVM.DLL to KDBAZIS.DLL to avoid name colision in Windows 8
	 Added support for VirtualBox 4.1.x
	 Added a tool for automatic integration with VirtualBox
v2.7 Fixed compatibility with Windows 8
v2.8 Fixed ASSERT() support.
	 VirtualBox integration is now done via pluggable device manager and does not require replacing VBoxDD.DLL	 
	 Fixed boot.ini installation bug
	 
v3.0 Added support for Windows 10 and VirtualBox 5.x
	
THANKS
I would like to thank the following people for making the creation of this tool possible:
	* Ken Johnson [http://www.nynaeve.net] for the idea (VMKD project).
	* OpenVMTools team [http://sourceforge.net/projects/open-vm-tools]
	* Tomasz Nowak [http://undocumented.ntinternals.net/]
	* MS and VMWare for creating scalable and flexible architectures
