// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"

std::wstring className = L"HwndWrapper[BF1ModTools;;";

// 根据类名模糊查找，返回是否找到布尔值
bool FindWindowByClassName(const std::wstring& className)
{
	HWND hwnd = nullptr;
	while ((hwnd = FindWindowExW(nullptr, hwnd, nullptr, nullptr)) != nullptr)
	{
		wchar_t buffer[256];
		GetClassNameW(hwnd, buffer, sizeof(buffer) / sizeof(wchar_t));

		// 使用wcsstr函数进行模糊匹配
		if (wcsstr(buffer, className.c_str()) != nullptr)
		{
			return true;
		}
	}

	return false;
}

DWORD WINAPI LoaderThread(LPVOID lpParam)
{
	// 查找战地1模组工具箱窗口
	bool isFindWin = FindWindowByClassName(className);
	if (!isFindWin)
		return S_OK;

	// 获取数据文件夹路径    
	PWSTR dataPath;
	HRESULT hr = SHGetKnownFolderPath(FOLDERID_ProgramData, 0, NULL, &dataPath);
	if (!SUCCEEDED(hr))
		return S_OK;

	// 构建 马恩DLL 文件路径
	std::filesystem::path dllPath = std::filesystem::path(dataPath) / "BF1ModTools" / "AppData" / "Marne" / "Marne.dll";

	// 检查文件是否存在
	if (!std::filesystem::exists(dllPath))
		return S_OK;

	// 加载 马恩Dll
	LoadLibrary(dllPath.wstring().c_str());

	return S_OK;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)LoaderThread, hModule, NULL, NULL);
		DisableThreadLibraryCalls(hModule);
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}
