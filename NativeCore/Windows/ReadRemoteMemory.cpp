#include <windows.h>
#include <sstream>
#include <vector>

#include "NativeCore.hpp"

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

static bool SendCommond(std::string command, std::string& result)
{
	std::string outputBuffer = "";
	CHAR tempOutputBuffer[1024];
	DWORD tempBytesRead = 0;
	DWORD bytesWritten = 0;

	if (command.empty())
	{
		return false;
	}

	if (command[command.size() - 1] != '\n')
	{
		command += '\n';
	}

	// 写数据
	if (!WriteFile(g_hChildStd_IN_Wr, command.data(), command.size(), &bytesWritten, NULL))
	{
		return false;
	}

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
				outputBuffer = outputBuffer.substr(0, pos);
				break;
			}
		}
	} while (true);

	result = outputBuffer;

	return true;
}

static std::vector<std::string> splitStringBySpace(const std::string& str)
{
	std::vector<std::string> tokens;
	std::istringstream iss(str);
	std::string token;
	while (iss >> token)
	{
		tokens.push_back(token);
	}
	return tokens;
}

static bool GetData(PVOID pStartAddress, SIZE_T length, PBYTE bytes)
{
	std::string command_prefix = Format(".for(r $t0 = 0x%llx; @$t0 < 0x%llx; r $t0 = @$t0 + 1)", (ULONG_PTR)pStartAddress, (ULONG_PTR)pStartAddress + length);
	std::string command_suffix = R"({.if($vvalid(@$t0, 1)){.printf @"%02x ", by(@$t0)}.else{.printf "?? "}};.printf "\n";)";
	std::string command = command_prefix + command_suffix;
	std::string result;

	if (!SendCommond(command, result))
	{
		return false;
	}

	std::vector<std::string> splitStringlist = splitStringBySpace(result);
	for (size_t i = 0; i < splitStringlist.size(); ++i)
	{
		if ("??" == splitStringlist[i])
		{
			return false;
		}
		else
		{
			BYTE byteValue = std::stoi(splitStringlist[i], nullptr, 16);
			bytes[i] = byteValue;
		}
	}

	return true;
}

bool RC_CallConv ReadRemoteMemory(RC_Pointer handle, RC_Pointer address, RC_Pointer buffer, int offset, int size)
{
	if (g_IsDumpAnalysis)
	{
		buffer = reinterpret_cast<RC_Pointer>(reinterpret_cast<uintptr_t>(buffer) + offset);
		return GetData(address, size, (PBYTE)buffer);
	}

	buffer = reinterpret_cast<RC_Pointer>(reinterpret_cast<uintptr_t>(buffer) + offset);

	SIZE_T numberOfBytesRead = 0;
	if (ReadProcessMemory(handle, address, buffer, size, &numberOfBytesRead) && (size == numberOfBytesRead))
	{
		return true;
	}

	return false;
}
