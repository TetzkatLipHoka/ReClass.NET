#include <windows.h>

#include "NativeCore.hpp"

HANDLE g_hCdbProcess = NULL;
HANDLE g_hChildStd_IN_Wr = NULL;
HANDLE g_hChildStd_OUT_Rd = NULL;
HANDLE g_hChildStd_ERR_Rd = NULL;
bool g_IsDumpAnalysis = false;

static std::wstring String2Wstring(const std::string& in, int code_page = 936)
{
	LPCSTR psz_src = in.c_str();
	int len = in.size();

	int size = MultiByteToWideChar(code_page, 0, (LPCSTR)psz_src, len, 0, 0);
	if (size <= 0)
		return L"";
	WCHAR* pwsz_dst = new WCHAR[size + 1];
	if (NULL == pwsz_dst)
		return L"";
	MultiByteToWideChar(code_page, 0, (LPCSTR)psz_src, len, pwsz_dst, size);
	pwsz_dst[size] = 0;
	if (pwsz_dst[0] == 0xFEFF)
	{
		for (int i = 0; i < size; i++)
			pwsz_dst[i] = pwsz_dst[i + 1];
	}
	std::wstring wstr(pwsz_dst);
	delete pwsz_dst;
	return wstr;
}

static std::string Format(const char* format, ...)
{
	va_list argptr;
	va_start(argptr, format);
	int count = _vsnprintf(NULL, 0, format, argptr);
	va_end(argptr);

	va_start(argptr, format);
	char* buf = (char*)malloc(count * sizeof(char));
	if (NULL == buf)
	{
		return "";
	}
	_vsnprintf(buf, count, format, argptr);
	va_end(argptr);

	std::string str(buf, count);
	free(buf);
	return str;
}

static std::wstring Format(const wchar_t* format, ...)
{
	va_list argptr;
	va_start(argptr, format);
	int count = _vsnwprintf(NULL, 0, format, argptr);
	va_end(argptr);

	va_start(argptr, format);
	wchar_t* buf = (wchar_t*)malloc(count * sizeof(wchar_t));
	if (NULL == buf)
	{
		return L"";
	}
	_vsnwprintf(buf, count, format, argptr);
	va_end(argptr);

	std::wstring str(buf, count);
	free(buf);
	return str;
}

static bool RunAppWithRedirection(
	const wchar_t* application, const wchar_t* command,
	HANDLE input, HANDLE output, HANDLE error, HANDLE* process)
{
	PROCESS_INFORMATION pi;
	STARTUPINFOW si;

	memset(&si, 0, sizeof(si));

	if (!!input || !!output || !!error)
	{
		si.dwFlags = STARTF_USESTDHANDLES;
	}

	si.cb = sizeof(si);
	si.hStdInput = input ? input : ::GetStdHandle(STD_INPUT_HANDLE);
	si.hStdOutput = output ? output : ::GetStdHandle(STD_OUTPUT_HANDLE);
	si.hStdError = error ? error : ::GetStdHandle(STD_ERROR_HANDLE);

	wchar_t* command_dup = wcsdup(command);

	if (::CreateProcessW(application,
						 command_dup,
						 NULL,
						 NULL,
						 TRUE,
						 CREATE_NO_WINDOW,
						 NULL,
						 NULL,
						 &si,
						 &pi))
	{
		::CloseHandle(pi.hThread);
		if (process == NULL)
		{
			::CloseHandle(pi.hProcess);
		}
		else
		{
			*process = pi.hProcess;
		}
		free(command_dup);
		return true;
	}

	free(command_dup);
	return false;
}

static bool InitCdb(std::wstring dump_path)
{
	HANDLE hChildStd_IN_Rd = NULL;
	HANDLE hChildStd_OUT_Wr = NULL;
	HANDLE hChildStd_ERR_Wr = NULL;

	std::string outputBuffer = "";
	CHAR tempOutputBuffer[1024];
	DWORD tempBytesRead = 0;

	// 设置安全属性，以便子进程可以继承句柄  
	SECURITY_ATTRIBUTES sa = { sizeof(SECURITY_ATTRIBUTES) };
	sa.bInheritHandle = TRUE;
	sa.lpSecurityDescriptor = NULL;

	// 创建管道用于标准输入  
	if (!CreatePipe(&hChildStd_IN_Rd, &g_hChildStd_IN_Wr, &sa, 0))
	{
		return false;
	}

	// 确保写句柄在子进程中不可见  
	if (!SetHandleInformation(g_hChildStd_IN_Wr, HANDLE_FLAG_INHERIT, 0))
	{
		return false;
	}

	// 创建管道用于标准输出  
	if (!CreatePipe(&g_hChildStd_OUT_Rd, &hChildStd_OUT_Wr, &sa, 0))
	{
		return false;
	}

	// 确保读句柄在子进程中不可见  
	if (!SetHandleInformation(g_hChildStd_OUT_Rd, HANDLE_FLAG_INHERIT, 0))
	{
		return false;
	}

	// 创建管道用于标准错误  
	if (!CreatePipe(&g_hChildStd_ERR_Rd, &hChildStd_ERR_Wr, &sa, 0))
	{
		return false;
	}

	// 确保读句柄在子进程中不可见  
	if (!SetHandleInformation(g_hChildStd_ERR_Rd, HANDLE_FLAG_INHERIT, 0))
	{
		return false;
	}

	// 创建进程
	std::wstring app_path = L"C:\\Program Files (x86)\\Windows Kits\\10\\Debuggers\\x64\\cdb.exe";
	std::wstring cmd_line = Format(L"\"%ws\" -z \"%ws\"", app_path.c_str(), dump_path.c_str());
	if (!RunAppWithRedirection(
		app_path.c_str(), cmd_line.c_str(),
		hChildStd_IN_Rd, hChildStd_OUT_Wr, hChildStd_ERR_Wr, &g_hCdbProcess))
	{
		return false;
	}

	// 关闭不需要的句柄
	CloseHandle(hChildStd_IN_Rd);
	hChildStd_IN_Rd = NULL;
	CloseHandle(hChildStd_OUT_Wr);
	hChildStd_OUT_Wr = NULL;
	CloseHandle(hChildStd_ERR_Wr);
	hChildStd_ERR_Wr = NULL;

	// 读数据
	do
	{
		if (!ReadFile(g_hChildStd_OUT_Rd, tempOutputBuffer, sizeof(tempOutputBuffer) - 1, &tempBytesRead, NULL))
		{
			break;
		}
		tempOutputBuffer[tempBytesRead] = '\0';
		outputBuffer += tempOutputBuffer;

		size_t pos = outputBuffer.find_last_of('\n');
		if (pos != std::string::npos)
		{
			std::string lastLineBuffer = outputBuffer.substr(pos + 1);
			if (lastLineBuffer.size() > 5 &&
				lastLineBuffer.size() < 15 &&
				' ' == lastLineBuffer[lastLineBuffer.size() - 1] &&
				'>' == lastLineBuffer[lastLineBuffer.size() - 2])
			{
				break;
			}
		}
	} while (true);

	if (hChildStd_IN_Rd)
	{
		CloseHandle(hChildStd_IN_Rd);
	}
	if (hChildStd_OUT_Wr)
	{
		CloseHandle(hChildStd_OUT_Wr);
	}
	if (hChildStd_ERR_Wr)
	{
		CloseHandle(hChildStd_ERR_Wr);
	}

	return true;
}

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

bool RC_CallConv OpenDumpFile(RC_Pointer dumpFilePath)
{
	if (g_IsDumpAnalysis)
	{
		if (!FinishCdb())
		{
			return false;
		}

		g_IsDumpAnalysis = false;
	}

	PCHAR tempStr = (PCHAR)dumpFilePath;
	std::wstring wDumpFilePath = String2Wstring(tempStr);
	if (!InitCdb(wDumpFilePath))
	{
		return false;
	}

	g_IsDumpAnalysis = true;

	return true;
}
