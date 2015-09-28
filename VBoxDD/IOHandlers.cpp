#include "stdafx.h"
#include "IOHandlers.h"

#include <VBox/vmm/mm.h>

int VirtualKDDevice::VirtualKDPortOutHandler( void *pvUser, RTIOPORT Port, uint32_t u32, unsigned cb )
{
	struct  
	{
		unsigned RequestSize;
		unsigned MaxReplySize;
	} RequestHeader = {0, };
	static char CmdBody[262144];
	if (Port == 0x5659)
	{
		int rc = PDMDevHlpPhysRead(m_pDevIns, (RTGCPHYS)u32, &RequestHeader, sizeof(RequestHeader));
		if (!RT_SUCCESS(rc) || !RequestHeader.RequestSize)
			return VINF_SUCCESS;
		rc = PDMDevHlpPhysRead(m_pDevIns, (RTGCPHYS)(u32 + sizeof(RequestHeader)), CmdBody, RequestHeader.RequestSize);
		if (!RT_SUCCESS(rc))
			return VINF_SUCCESS;
		
		char *pReply = NULL;
		unsigned done = m_pKDClient ? m_pKDClient->OnRawRequest(CmdBody, RequestHeader.RequestSize, &pReply) : 0;

		if (!pReply)
			done = 0;

		char Prefix[sizeof(done) + 2];
		((unsigned *)Prefix)[0] = done + 2;
		Prefix[sizeof(unsigned)] = '1';
		Prefix[sizeof(unsigned) + 1] = ' ';

		rc = PDMDevHlpPhysWrite(m_pDevIns, (RTGCPHYS)u32, Prefix, sizeof(Prefix));
		if (!RT_SUCCESS(rc))
			return VINF_SUCCESS;
		if (done)
		{
			rc = PDMDevHlpPhysWrite(m_pDevIns, (RTGCPHYS)(u32 + sizeof(Prefix)), pReply, done);
			if (!RT_SUCCESS(rc))
				return VINF_SUCCESS;
		}
		return VINF_SUCCESS;
	}
	else
	{
		if ((Port == 0x5658) && (u32 == 0x564D5868))
			m_bVMWareOpenChannelDetected = true;
		else
			m_bVMWareOpenChannelDetected = false;
		return VINF_SUCCESS;
	}
}

int VirtualKDDevice::VirtualKDPortInHandler( void *pvUser, RTIOPORT Port, uint32_t *pu32, unsigned cb )
{
	if (m_bVMWareOpenChannelDetected)
	{
		*pu32 = 'XOBV';	//Checked in VMWRPC.H
		m_bVMWareOpenChannelDetected = false;
		m_bChannelDetectSuccessful = true;
	}
	else
		*pu32 = -1;
	return VINF_SUCCESS;
}

int VirtualKDDevice::sVirtualKDPortInHandler( PPDMDEVINS pDevIns, void *pvUser, RTIOPORT Port, uint32_t *pu32, unsigned cb )
{
	VirtualKDDevice *pThis = PDMINS_2_DATA(pDevIns, VirtualKDDevice *);
	return pThis->VirtualKDPortInHandler(pvUser, Port, pu32, cb);
}

int VirtualKDDevice::sVirtualKDPortOutHandler( PPDMDEVINS pDevIns, void *pvUser, RTIOPORT Port, uint32_t u32, unsigned cb )
{
	VirtualKDDevice *pThis = PDMINS_2_DATA(pDevIns, VirtualKDDevice *);
	return pThis->VirtualKDPortOutHandler(pvUser, Port, u32, cb);
}

//-------------------------------------------------------------------------------------------------------------------------------------

int VirtualKDDevice::RegisterHandlers()
{
	int rc = PDMDevHlpIOPortRegister(m_pDevIns, 0x5658, 2, NULL,  &sVirtualKDPortOutHandler, &sVirtualKDPortInHandler, NULL, NULL, "VirtualKD interface");
	if (!RT_SUCCESS(rc))
		return rc;
	return VINF_SUCCESS;
}

VirtualKDDevice::~VirtualKDDevice()
{
	delete m_pKDClient;
	if (m_hKDClient)
		FreeLibrary(m_hKDClient);
}

VirtualKDDevice::VirtualKDDevice(PPDMDEVINS pDevIns, const TCHAR *pVirtualKDPath) 
	: m_bVMWareOpenChannelDetected(false)
	, m_bChannelDetectSuccessful(false)
	, m_hKDClient(0)
	, m_pKDClient(NULL)
	, m_pDevIns(pDevIns)
{
	extern HINSTANCE g_hThisDll;

	TCHAR tszDLL[MAX_PATH] = {0,};
	_tcscpy_s(tszDLL, pVirtualKDPath);

#ifdef _WIN64
	_tcscat_s(tszDLL, _T("\\kdclient64.dll"));
#else
	_tcscat_s(tszDLL, _T("\\kdclient.dll"));
#endif

	m_hKDClient = LoadLibrary(tszDLL);
	if (m_hKDClient)
	{
		PCreateVBoxKDClientEx init = (PCreateVBoxKDClientEx)GetProcAddress(m_hKDClient, "CreateVBoxKDClientEx");
		if (init)
		{
			m_pKDClient = init(IKDClient::InterfaceVersion);
			if (m_pKDClient)
			{
				return;
			}
		}
	}

	TCHAR tszError[MAX_PATH + 512];
	_sntprintf_s(tszError, _TRUNCATE, _T("VirtualKD failed to load %s. Fast kernel-mode debugging won't work!"), tszDLL);
	MessageBox(0, tszError, NULL, MB_ICONERROR);
}

