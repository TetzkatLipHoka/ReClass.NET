#include <windows.h>

#include "NativeCore.hpp"

static bool FinishCdb()
{
	std::string command = "q\n";
	if (!WriteFile(g_hChildStd_IN_Wr, command.data(), command.size(), NULL, NULL))
	{
		return false;
	}

	if (g_hChildStd_IN_Wr)
	{
		CloseHandle(g_hChildStd_IN_Wr);
	}
	if (g_hChildStd_OUT_Rd)
	{
		CloseHandle(g_hChildStd_OUT_Rd);
	}
	if (g_hChildStd_ERR_Rd)
	{
		CloseHandle(g_hChildStd_ERR_Rd);
	}

	if (g_hCdbProcess)
	{
		CloseHandle(g_hCdbProcess);
	}

	return true;
}

void RC_CallConv CloseRemoteProcess(RC_Pointer handle)
{
	/*
	if (g_IsDumpAnalysis)
	{
		FinishCdb();
	}
	else
	{
		if (handle == nullptr)
		{
			return;
		}

		CloseHandle(handle);
	}
	*/

	if (handle == nullptr)
	{
		return;
	}

	CloseHandle(handle);
}
