#include "HideMyWindows.h"
#include <MinHook.h>

// Callbacks
void OnWindowCreated(HWND hwnd) {
    if (isActivelyHiding) {
        HmwSetWindowVisibility(hwnd, false);
    }
}

DWORD GetSettableAffinityState(HWND hwnd, DWORD dwAffinity) {
    if (isActivelyHiding) {
        return WDA_EXCLUDEFROMCAPTURE;
    }
    else {
        return dwAffinity;
    }
}

// CreateWindowEx
typedef HWND(WINAPI* CreateWindowExAType)(
    _In_     DWORD     dwExStyle,
    _In_opt_ LPCSTR    lpClassName,
    _In_opt_ LPCSTR    lpWindowName,
    _In_     DWORD     dwStyle,
    _In_     int       X,
    _In_     int       Y,
    _In_     int       nWidth,
    _In_     int       nHeight,
    _In_opt_ HWND      hWndParent,
    _In_opt_ HMENU     hMenu,
    _In_opt_ HINSTANCE hInstance,
    _In_opt_ LPVOID    lpParam
);

CreateWindowExAType fpCreateWindowExA = NULL;

HWND WINAPI DetourCreateWindowExA(
    _In_     DWORD     dwExStyle,
    _In_opt_ LPCSTR    lpClassName,
    _In_opt_ LPCSTR    lpWindowName,
    _In_     DWORD     dwStyle,
    _In_     int       X,
    _In_     int       Y,
    _In_     int       nWidth,
    _In_     int       nHeight,
    _In_opt_ HWND      hWndParent,
    _In_opt_ HMENU     hMenu,
    _In_opt_ HINSTANCE hInstance,
    _In_opt_ LPVOID    lpParam
) {
    HWND hwnd = fpCreateWindowExA(dwExStyle, lpClassName, lpWindowName, dwStyle, X, Y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
    OnWindowCreated(hwnd);

    return hwnd;
}

typedef HWND(WINAPI* CreateWindowExWType)(
    _In_     DWORD     dwExStyle,
    _In_opt_ LPCWSTR   lpClassName,
    _In_opt_ LPCWSTR   lpWindowName,
    _In_     DWORD     dwStyle,
    _In_     int       X,
    _In_     int       Y,
    _In_     int       nWidth,
    _In_     int       nHeight,
    _In_opt_ HWND      hWndParent,
    _In_opt_ HMENU     hMenu,
    _In_opt_ HINSTANCE hInstance,
    _In_opt_ LPVOID    lpParam
);

CreateWindowExWType fpCreateWindowExW = NULL;

HWND WINAPI DetourCreateWindowExW(
    _In_     DWORD     dwExStyle,
    _In_opt_ LPCWSTR   lpClassName,
    _In_opt_ LPCWSTR   lpWindowName,
    _In_     DWORD     dwStyle,
    _In_     int       X,
    _In_     int       Y,
    _In_     int       nWidth,
    _In_     int       nHeight,
    _In_opt_ HWND      hWndParent,
    _In_opt_ HMENU     hMenu,
    _In_opt_ HINSTANCE hInstance,
    _In_opt_ LPVOID    lpParam
) {
    HWND hwnd = fpCreateWindowExW(dwExStyle, lpClassName, lpWindowName, dwStyle, X, Y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
    OnWindowCreated(hwnd);

    return hwnd;
}

// SetWindowDisplayAffinity
SetWindowDisplayAffinityType fpSetWindowDisplayAffinity = NULL;

BOOL WINAPI DetourSetWindowDisplayAffinity(
    _In_ HWND  hWnd,
    _In_ DWORD dwAffinity
) {
    DWORD newAffinity = GetSettableAffinityState(hWnd, dwAffinity);
    return fpSetWindowDisplayAffinity(hWnd, newAffinity);
}

void HmwSetupHooks()
{
    MH_Initialize();
    MH_CreateHookApi(L"user32.dll", "CreateWindowExA", &DetourCreateWindowExA, (LPVOID*)&fpCreateWindowExA);
    MH_EnableHook(&CreateWindowExA);
    MH_CreateHookApi(L"user32.dll", "CreateWindowExW", &DetourCreateWindowExW, (LPVOID*)&fpCreateWindowExW);
    MH_EnableHook(&CreateWindowExW);
    MH_CreateHookApi(L"user32.dll", "SetWindowDisplayAffinity", &DetourSetWindowDisplayAffinity, (LPVOID*)&fpSetWindowDisplayAffinity);
    MH_EnableHook(&SetWindowDisplayAffinity);
    // TODO: Child process hooking currently not implemented, consider re-implementing? Not sure if it's a good idea though. (in the future maybe hook NtResumeThread?)
    //MH_CreateHookApi(L"kernel32.dll", "CreateProcessA", &DetourCreateProcessA, (LPVOID*)&fpCreateProcessA);
    //MH_EnableHook(&CreateProcessA);
    //MH_CreateHookApi(L"kernel32.dll", "CreateProcessW", &DetourCreateProcessW, (LPVOID*)&fpCreateProcessW);
    //MH_EnableHook(&CreateProcessW);
}