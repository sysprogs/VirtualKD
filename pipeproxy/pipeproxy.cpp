/*! \file pipeproxy.cpp
    \author Ivan Shcherbakov (Bazis)
    $Id: pipeproxy.cpp,v 1.11 2009-06-07 15:31:55 Administrator Exp $
    \brief Contains the entry point and implementation for Pipe proxy used for debugging
*/

#include "stdafx.h"
#include <bzscore/cmndef.h>
#include "../kdvm/kdrpc.h"
#include "../kdvm/kdxxx.h"
#include "../rpcdispatch/pipesrv.h"
#include "../rpcdispatch/rpcdisp.h"
#include "../rpcdispatch/kdcomdisp.h"
#include "../rpcdispatch/reporter.h"

#ifndef KDVMWARE_USE_PROXY
#error KDVMWARE_USE_PROXY should be defined in project properties for headers to work correctly
#endif

static const TCHAR tszProxyPipeName[] = _T("\\\\.\\pipe\\kd_proxy");

//! Processes \ref kdrpc_proto KDRPC data packets received from KDCLIENT.DLL
/*! The RpcProxy class implements a \ref pipeproxy "Named Pipe Proxy" functionality to simplify debugging.
	A typical call sequence with RpcProxy enabled is the following:
	\msc
	Kernel[textcolor="#FF0000"],"KDVMWARE.DLL"[textcolor="#FF0000"],RpcProxy[textcolor="#0000FF"],KdRpcDispatcher[textcolor="#008000"],KdComDispatcher[textcolor="#008000"],WinDBG;
	Kernel=>"KDVMWARE.DLL" [label="KdSendPacket()", URL="\ref KdSendPacket()"];
	"KDVMWARE.DLL"=>RpcProxy [label="Marshal params", URL="\ref KdSendPacket()"];
	RpcProxy=>KdRpcDispatcher [label="Forward params", URL="\ref KdSendPacket()"];
	KdRpcDispatcher=>KdComDispatcher  [label="Unmarshal params", URL="\ref KdComDispatcher::KdSendPacket()"];
	KdComDispatcher=>WinDBG  [label="KDCOM packet", URL="\ref KdComDispatcher::KdSendPacket()"];
	KdComDispatcher<=WinDBG  [label="ACK packet", URL="\ref KdComDispatcher::KdSendPacket()"];
	KdRpcDispatcher<=KdComDispatcher  [label="Return control", URL="\ref KdComDispatcher::KdSendPacket()"];
	RpcProxy<=KdRpcDispatcher [label="Marshal result", URL="\ref KdSendPacket()"];
	"KDVMWARE.DLL"<=RpcProxy[label="Forward result", URL="\ref KdSendPacket()"];
	Kernel<="KDVMWARE.DLL" [label="Return control", URL="\ref KdSendPacket()"];
	\endmsc
	The color of the entries specifies whether they execute in kernel mode on guest (red), in
	the address space of VMWARE-VMX.EXE (blue), or as a separate process started by Visual Studio(green).

*/
class RpcProxy
{
private:
	enum {TEMP_BUF_SIZE = 262144};
	PipeServer<true, false> m_Server;
	KdRpcDispatcher m_Client;

	char *m_pBuf1;

public:
	RpcProxy()
		: m_Server(_T("\\\\.\\pipe\\kdvmware_proxypipe"))
		, m_Client(new KdComDispatcher(tszProxyPipeName))
	{
		m_pBuf1 = new char[TEMP_BUF_SIZE];
		wcsncpy(g_pReporter->GetStatusPointer()->PipeName, tszProxyPipeName, __countof(g_pReporter->GetStatusPointer()->PipeName));
		g_pReporter->GetStatusPointer()->PatchErrorPlus1 = 1;
	}

	~RpcProxy()
	{
		delete m_pBuf1;
	}

	void MainLoop()
	{
		for (;;)
		{
			size_t done = m_Server.Receive(m_pBuf1, TEMP_BUF_SIZE);
			if (!done)
			{
				if (m_Server.ReconnectPending())
				{
					m_Server.ReconnectPipe();
					continue;
				}
			}
			char *pReply = NULL;
			size_t replySize = m_Client.OnRequest(m_pBuf1, done, &pReply);
			if (!m_Server.Send(pReply, replySize))
			{
				if (m_Server.ReconnectPending())
				{
					m_Server.ReconnectPipe();
					continue;
				}
			}
		}
	}
};

StatusReporter *g_pReporter = NULL;

int _tmain(int argc, _TCHAR* argv[])
{
	g_pReporter = new StatusReporter();
	RpcProxy proxy;
	proxy.MainLoop();
	return 0;
}

