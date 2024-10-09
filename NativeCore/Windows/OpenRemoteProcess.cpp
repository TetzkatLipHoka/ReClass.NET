#include <windows.h>
#include <unordered_map>

#include "NativeCore.hpp"

RC_Pointer RC_CallConv OpenRemoteProcess(RC_Pointer id, ProcessAccess desiredAccess)
{
	if (g_IsDumpAnalysis)
	{
		static std::unordered_map<RC_Pointer, DWORD> id_handle_map = {};
		static DWORD process_handle = 0;

		if (id_handle_map.end() == id_handle_map.find(id))
		{
			id_handle_map[id] = ++process_handle;
		}

		return (RC_Pointer)id_handle_map[id];
	}

	DWORD access = STANDARD_RIGHTS_REQUIRED | PROCESS_TERMINATE | PROCESS_QUERY_INFORMATION | SYNCHRONIZE;
	switch (desiredAccess)
	{
		case ProcessAccess::Read:
			access |= PROCESS_VM_READ;
			break;
		case ProcessAccess::Write:
			access |= PROCESS_VM_OPERATION | PROCESS_VM_WRITE;
			break;
		case ProcessAccess::Full:
			access |= PROCESS_VM_READ | PROCESS_VM_OPERATION | PROCESS_VM_WRITE;
			break;
	}

	const auto handle = OpenProcess(access, FALSE, static_cast<DWORD>(reinterpret_cast<size_t>(id)));

	if (handle == nullptr || handle == INVALID_HANDLE_VALUE)
	{
		return nullptr;
	}

	return handle;
}
