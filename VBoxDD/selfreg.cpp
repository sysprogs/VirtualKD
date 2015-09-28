#include "stdafx.h"
#include <atlbase.h>
#include <atlcomcli.h>
#include "../kdclient/vmapi/VirtualBox.h"

extern HINSTANCE g_hThisDll;

HRESULT __stdcall DllRegisterServer()
{
	CComPtr<IVirtualBox> pVirtualBox;
	HRESULT hR = pVirtualBox.CoCreateInstance(CLSID_VirtualBox, NULL, CLSCTX_LOCAL_SERVER);
	if (!SUCCEEDED(hR))
		return hR;

	TCHAR tszDLL[MAX_PATH] = {0,};
	GetModuleFileName(g_hThisDll, tszDLL, _countof(tszDLL));
	for (TCHAR *p = tszDLL; *p; p++)
		if (p[0] == '\\')
			p[0] = '/';

	hR = pVirtualBox->SetExtraData(SysAllocString(L"VBoxInternal/PDM/Devices/VirtualKD/Path"), SysAllocString(tszDLL));
	if (!SUCCEEDED(hR))
		return hR;

	hR = pVirtualBox->SetExtraData(SysAllocString(L"VBoxInternal/Devices/VirtualKD/0/Name"), SysAllocString(L"Default"));
	if (!SUCCEEDED(hR))
		return hR;

	return S_OK;
}

HRESULT __stdcall DllUnregisterServer()
{
	CComPtr<IVirtualBox> pVirtualBox;
	HRESULT hR = pVirtualBox.CoCreateInstance(CLSID_VirtualBox, NULL, CLSCTX_LOCAL_SERVER);
	if (!SUCCEEDED(hR))
		return hR;

	TCHAR tszDLL[MAX_PATH] = {0,};
	GetModuleFileName(g_hThisDll, tszDLL, _countof(tszDLL));
	for (TCHAR *p = tszDLL; *p; p++)
		if (p[0] == '\\')
			p[0] = '/';

	hR = pVirtualBox->SetExtraData(SysAllocString(L"VBoxInternal/PDM/Devices/VirtualKD/Path"), NULL);
	if (!SUCCEEDED(hR))
		return hR;

	hR = pVirtualBox->SetExtraData(SysAllocString(L"VBoxInternal/Devices/VirtualKD/0/Name"), NULL);
	if (!SUCCEEDED(hR))
		return hR;

	return S_OK;
}
