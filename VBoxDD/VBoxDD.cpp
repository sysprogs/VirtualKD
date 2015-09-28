// VBoxDD.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "IOHandlers.h"
#include <new.h>

static int RTCALL kdConstruct(PPDMDEVINS pDevIns, int iInstance, PCFGMNODE pCfgHandle)
{
	VirtualKDDevice *pThis = PDMINS_2_DATA(pDevIns, VirtualKDDevice *);
	const TCHAR *pVirtualKDPath = _T("c:\\test\\VirtualKD");	//TODO: load VirtualKD path from VirtualBox configuration items
	pThis = new(pThis) VirtualKDDevice(pDevIns, pVirtualKDPath);

	return pThis->RegisterHandlers();
}

static int RTCALL kdDestruct(PPDMDEVINS pDevIns)
{
	VirtualKDDevice *pThis = PDMINS_2_DATA(pDevIns, VirtualKDDevice *);
	pThis->~VirtualKDDevice();
	return VINF_SUCCESS;
}

static const PDMDEVREG s_VirtualKDDevice =
{
	/* u32Version */
	PDM_DEVREG_VERSION,
	/* szName */
	"VirtualKD",
	/* szRCMod */
	"",
	/* szR0Mod */
	"",
	/* pszDescription */
	"Provides fast debugging interface when debugging Windows kernel",
	/* fFlags */
	PDM_DEVREG_FLAGS_DEFAULT_BITS,
	/* fClass */
	PDM_DEVREG_CLASS_MISC,
	/* cMaxInstances */
	1,
	/* cbInstance */
	sizeof(VirtualKDDevice),
	/* pfnConstruct */
	kdConstruct,
	/* pfnDestruct */
	kdDestruct,
	/* pfnRelocate */
	NULL,
	/* pfnIOCtl */
	NULL,
	/* pfnPowerOn */
	NULL,
	/* pfnReset */
	NULL,
	/* pfnSuspend */
	NULL,
	/* pfnResume */
	NULL,
	/* pfnAttach */
	NULL,
	/* pfnDetach */
	NULL,
	/* pfnQueryInterface */
	NULL,
	/* pfnInitComplete */
	NULL,
	/* pfnPowerOff */
	NULL,
	/* pfnSoftReset */
	NULL,
	/* u32VersionEnd */
	PDM_DEVREG_VERSION
};

extern "C"	int VBoxDevicesRegister(PPDMDEVREGCB pCallbacks, uint32_t u32Version)
{
	int rc = pCallbacks->pfnRegister(pCallbacks, &s_VirtualKDDevice);
	return rc;
}
