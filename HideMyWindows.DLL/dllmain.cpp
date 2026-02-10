// NOTE: When modifying only the dll, you will need to rebuild it for all architectures because Visual Studio will not build them automatically.
#include "HideMyWindows.h"

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        HmwSetupHooks();
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

// Hide / Unhide window
struct HideWindowParameter
{
    HWND hwnd;
};

extern "C" __declspec(dllexport) void HideWindow(HideWindowParameter* param) {
    HmwSetWindowVisibility(param->hwnd, false);
}

extern "C" __declspec(dllexport) void UnhideWindow(HideWindowParameter* param) {
    HmwSetWindowVisibility(param->hwnd, true);
}

// Hide / Unhide all windows
extern "C" __declspec(dllexport) void HideAllWindows() {
    HmwSetAllWindowsVisibility(false);
}

extern "C" __declspec(dllexport) void UnhideAllWindows() {
    HmwSetAllWindowsVisibility(true);
}

// Hide / Unhide tray icon
struct HideTrayIconParameter
{
    HWND hwnd;
};

extern "C" __declspec(dllexport) void HideTrayIcon(HideTrayIconParameter * param) {
    HmwSetTrayIconVisibility(param->hwnd, false);
}

extern "C" __declspec(dllexport) void UnhideTrayIcon(HideTrayIconParameter * param) {
    HmwSetTrayIconVisibility(param->hwnd, true);
}
