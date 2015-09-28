#pragma once 

#ifdef _WIN64
#define VBOX_WITH_64_BITS_GUESTS
#endif

#define IN_RING3
#include <VBox/vmm/pdmdev.h>
#include <VBox/vmm/pdmdrv.h>
#include <VBox/vmm/pdmusb.h>
#include <VBox/vmm/pgm.h>

class IKDClient
{
public:
	virtual unsigned OnRawRequest(const char *pRequestIncludingRpcHeader, unsigned RequestSizeWithRpcHeader, char **ppReply)=0;
	virtual ~IKDClient() {}

	enum {InterfaceVersion = 3};
};


class VirtualKDDevice
{
private:
	typedef IKDClient *(*PCreateVBoxKDClientEx)(unsigned version);

private:
	bool m_bVMWareOpenChannelDetected;
	bool m_bChannelDetectSuccessful;

	HMODULE m_hKDClient;
	IKDClient *m_pKDClient;
	PPDMDEVINS m_pDevIns;

private:
	int VirtualKDPortOutHandler(void *pvUser, RTIOPORT Port, uint32_t u32, unsigned cb);
	int VirtualKDPortInHandler(void *pvUser, RTIOPORT Port, uint32_t *pu32, unsigned cb);

private:
	static int sVirtualKDPortOutHandler(PPDMDEVINS pDevIns, void *pvUser, RTIOPORT Port, uint32_t u32, unsigned cb);
	static int sVirtualKDPortInHandler(PPDMDEVINS pDevIns, void *pvUser, RTIOPORT Port, uint32_t *pu32, unsigned cb);

public:
	VirtualKDDevice(PPDMDEVINS pDevIns, const TCHAR *pVirtualKDPath);

	int RegisterHandlers();
	~VirtualKDDevice();
};

