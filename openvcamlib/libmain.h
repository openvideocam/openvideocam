#ifndef _LIBMAINH_
#define _LIBMAINH_

#ifndef LINUX
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved);
#endif

#endif

