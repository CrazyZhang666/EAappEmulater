#pragma once

#define WIN32_LEAN_AND_MEAN             // 从 Windows 头文件中排除极少使用的内容
// Windows 头文件
#include <windows.h>

#include <string>
#include <filesystem>
#include <ShlObj_core.h>

#pragma comment(linker, "/export:DirectInput8Create=C:\\\\Windows\\\\System32\\\\dinput8.dll.DirectInput8Create")
#pragma comment(linker, "/export:DllCanUnloadNow=C:\\\\Windows\\\\System32\\\\dinput8.dll.DllCanUnloadNow,PRIVATE")
#pragma comment(linker, "/export:DllGetClassObject=C:\\\\Windows\\\\System32\\\\dinput8.dll.DllGetClassObject,PRIVATE")
#pragma comment(linker, "/export:DllRegisterServer=C:\\\\Windows\\\\System32\\\\dinput8.dll.DllRegisterServer,PRIVATE")
#pragma comment(linker, "/export:DllUnregisterServer=C:\\\\Windows\\\\System32\\\\dinput8.dll.DllUnregisterServer,PRIVATE")
#pragma comment(linker, "/export:GetdfDIJoystick=C:\\\\Windows\\\\System32\\\\dinput8.dll.GetdfDIJoystick")
