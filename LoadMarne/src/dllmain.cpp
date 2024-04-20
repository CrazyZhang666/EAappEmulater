// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"

DWORD WINAPI LoaderThread(LPVOID lpParam)
{
	LoadLibraryA(std::string(".\\Marne.dll").c_str());

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
