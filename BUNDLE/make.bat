REM @echo off

set DEVENVPATH=%VS100COMNTOOLS%\..\IDE

if a%1 == adebug goto debug
if a%1 == anopdb goto nopdb
goto release

:debug
set CONFIGNAME=Debug
set TARGETSUBPATH=Debug\
mkdir Debug
goto build

:nopdb
set CONFIGNAME=Release
set TARGETSUBPATH=nopdb\
mkdir nopdb
goto build

:release
echo Building RELEASE configuration. Use "make debug" to build DEBUG configuration
set CONFIGNAME=Release
set TARGETSUBPATH=

:build

mkdir %TARGETSUBPATH%target
mkdir %TARGETSUBPATH%target\x86
mkdir %TARGETSUBPATH%target\x64

cd ..\kdclient
"%DEVENVPATH%\devenv" kdclient.sln /Build "%CONFIGNAME%|Win32"
if not exist %CONFIGNAME%\kdclient.dll goto error
if not exist %CONFIGNAME%\vmxpatch.exe goto error
"%DEVENVPATH%\devenv" kdclient.sln /Build "%CONFIGNAME%|x64"
if not exist x64\%CONFIGNAME%\kdclient64.dll goto error
if not exist x64\%CONFIGNAME%\vmxpatch64.exe goto error

copy /y %CONFIGNAME%\kdclient.dll ..\bundle\%TARGETSUBPATH%
copy /y %CONFIGNAME%\kdclient.pdb ..\bundle\%TARGETSUBPATH%
copy /y %CONFIGNAME%\vmxpatch.exe ..\bundle\%TARGETSUBPATH%
copy /y %CONFIGNAME%\vmxpatch.pdb ..\bundle\%TARGETSUBPATH%

copy /y x64\%CONFIGNAME%\kdclient64.dll ..\bundle\%TARGETSUBPATH%
copy /y x64\%CONFIGNAME%\kdclient64.pdb ..\bundle\%TARGETSUBPATH%
copy /y x64\%CONFIGNAME%\vmxpatch64.exe ..\bundle\%TARGETSUBPATH%
copy /y x64\%CONFIGNAME%\vmxpatch64.pdb ..\bundle\%TARGETSUBPATH%

cd ..\vmmon

"%DEVENVPATH%\devenv" vmmon.sln /Build "%CONFIGNAME%|Win32"
if not exist %CONFIGNAME%\vmmon.exe goto error
"%DEVENVPATH%\devenv" vmmon.sln /Build "%CONFIGNAME%|x64"
if not exist x64\%CONFIGNAME%\vmmon64.exe goto error

copy /y %CONFIGNAME%\vmmon.exe ..\bundle\%TARGETSUBPATH%
copy /y %CONFIGNAME%\vmmon.pdb ..\bundle\%TARGETSUBPATH%
copy /y x64\%CONFIGNAME%\vmmon64.exe ..\bundle\%TARGETSUBPATH%
copy /y x64\%CONFIGNAME%\vmmon64.pdb ..\bundle\%TARGETSUBPATH%

cd ..\kdpatch
copy /y kdpatch.reg ..\bundle\%TARGETSUBPATH%target

"%DEVENVPATH%\devenv" kdpatch.sln /Build "%CONFIGNAME%|Win32"
if not exist %CONFIGNAME%\kdbazis.dll goto error
if not exist %CONFIGNAME%\kdpatch.sys goto error

copy /y %CONFIGNAME%\kdbazis.dll ..\bundle\%TARGETSUBPATH%target\x86
copy /y %CONFIGNAME%\kdvm.pdb ..\bundle\%TARGETSUBPATH%target\x86
copy /y %CONFIGNAME%\kdpatch.sys ..\bundle\%TARGETSUBPATH%target\x86
copy /y %CONFIGNAME%\kdpatch.pdb ..\bundle\%TARGETSUBPATH%target\x86

"%DEVENVPATH%\devenv" kdpatch.sln /Build "%CONFIGNAME%|x64"
if not exist x64\%CONFIGNAME%\kdbazis.dll goto error
if not exist x64\%CONFIGNAME%\kdpatch.sys goto error

copy /y x64\%CONFIGNAME%\kdbazis.dll ..\bundle\%TARGETSUBPATH%target\x64
copy /y x64\%CONFIGNAME%\kdvm.pdb ..\bundle\%TARGETSUBPATH%target\x64
copy /y x64\%CONFIGNAME%\kdpatch.sys ..\bundle\%TARGETSUBPATH%target\x64
copy /y x64\%CONFIGNAME%\kdpatch.pdb ..\bundle\%TARGETSUBPATH%target\x64

REM cd ..\VBoxDD
REM "%DEVENVPATH%\devenv" VBoxDD.sln /Build "%CONFIGNAME%|Win32"
REM if not exist %CONFIGNAME%\VBoxKD.dll goto error
REM "%DEVENVPATH%\devenv" VBoxDD.sln /Build "%CONFIGNAME%|x64"
REM if not exist x64\%CONFIGNAME%\VBoxKD64.dll goto error

copy /y %CONFIGNAME%\VBoxKD.dll ..\bundle\%TARGETSUBPATH%
copy /y %CONFIGNAME%\VBoxKD.pdb ..\bundle\%TARGETSUBPATH%
copy /y x64\%CONFIGNAME%\VBoxKD64.dll ..\bundle\%TARGETSUBPATH%
copy /y x64\%CONFIGNAME%\VBoxKD64.pdb ..\bundle\%TARGETSUBPATH%

cd ..\vminstall
"%DEVENVPATH%\devenv" vminstall.sln /Build "%CONFIGNAME%|Win32"
if not exist %CONFIGNAME%\vminstall.exe goto error

copy /y %CONFIGNAME%\vminstall.exe ..\bundle\%TARGETSUBPATH%target

cd ..\VirtualKDSetup
"%DEVENVPATH%\devenv" VirtualKDSetup.sln /Build Release
if not exist bin\Release\VirtualKDSetup.exe goto error

copy /y bin\Release\VirtualKDSetup.exe ..\bundle\%TARGETSUBPATH%
copy /y bin\Release\Interop.VirtualBox.dll ..\bundle\%TARGETSUBPATH%
copy /y bin\Release\ICSharpCode.SharpZipLib.dll ..\bundle\%TARGETSUBPATH%

cd ..\bundle
call ..\..\..\utils\sign.bat %TARGETSUBPATH%target\x86\kdbazis.dll
call ..\..\..\utils\sign.bat %TARGETSUBPATH%target\x86\kdpatch.sys
call ..\..\..\utils\sign.bat %TARGETSUBPATH%target\x64\kdbazis.dll
call ..\..\..\utils\sign.bat %TARGETSUBPATH%target\x64\kdpatch.sys

call ..\..\..\utils\sign_r.bat %TARGETSUBPATH%target\vminstall.exe
call ..\..\..\utils\sign_r.bat %TARGETSUBPATH%kdclient.dll
call ..\..\..\utils\sign_r.bat %TARGETSUBPATH%kdclient64.dll


REM copy ..\database\*.vmpatch ..\bundle\%TARGETSUBPATH%

cd %TARGETSUBPATH%

cipher /d /a /s:.
if a%1 == adebug goto end
if a%1 == anopdb goto nopdb

goto end

:nopdb
del *.pdb
del target\x86\*.pdb
del target\x64\*.pdb
del VirtualBox\x86\*.pdb
del VirtualBox\x64\*.pdb
goto end

:error
echo Build failed!
pause

:end