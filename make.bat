call "%VS100COMNTOOLS%\..\..\VC\vcvarsall.bat"
echo on
msbuild /m VirtualKD.sln /property:Platform=Win32 /property:Configuration="Release (Kernel-mode)" || goto error
msbuild /m VirtualKD.sln /property:Platform=x64 /property:Configuration="Release (Kernel-mode)" || goto error
msbuild /m VirtualKD.sln /property:Platform=Win32 /property:Configuration="Release (User-mode)" || goto error
msbuild /m VirtualKD.sln /property:Platform=x64 /property:Configuration="Release (User-mode)" || goto error

goto end
:error
echo Build failed!
pause

:end
pause