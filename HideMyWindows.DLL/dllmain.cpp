// dllmain.cpp : Defines the entry point for the DLL application.
#include <windows.h>
#include <tlhelp32.h>
#include <stdio.h>
#include <MinHook.h>

void HideWindow(HWND hwnd) {
    SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
}

// Existing windows
BOOL CALLBACK EnumThreadWndProc(
    HWND   hwnd,
    LPARAM lParam
) {
    HideWindow(hwnd);
    return TRUE;
}

BOOL HideAllProcessWindows(DWORD dwOwnerPID)
{
    HANDLE hThreadSnap = INVALID_HANDLE_VALUE;
    THREADENTRY32 te32;

    hThreadSnap = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if (hThreadSnap == INVALID_HANDLE_VALUE)
        return(FALSE);

    te32.dwSize = sizeof(THREADENTRY32);

    if (!Thread32First(hThreadSnap, &te32))
    {
        CloseHandle(hThreadSnap);
        return(FALSE);
    }

    do
    {
        if (te32.th32OwnerProcessID == dwOwnerPID)
        {
            EnumThreadWindows(te32.th32ThreadID, EnumThreadWndProc, 0);
        }
    } while (Thread32Next(hThreadSnap, &te32));

    CloseHandle(hThreadSnap);
    return(TRUE);
}

// New windows
typedef HWND(*CreateWindowExAType)(
    DWORD     dwExStyle,
    LPCSTR    lpClassName,
    LPCSTR    lpWindowName,
    DWORD     dwStyle,
    int       X,
    int       Y,
    int       nWidth,
    int       nHeight,
    HWND      hWndParent,
    HMENU     hMenu,
    HINSTANCE hInstance,
    LPVOID    lpParam
);

CreateWindowExAType fpCreateWindowExA = NULL;

HWND DetourCreateWindowExA(
    DWORD     dwExStyle,
    LPCSTR    lpClassName,
    LPCSTR    lpWindowName,
    DWORD     dwStyle,
    int       X,
    int       Y,
    int       nWidth,
    int       nHeight,
    HWND      hWndParent,
    HMENU     hMenu,
    HINSTANCE hInstance,
    LPVOID    lpParam
) {
    HWND hwnd = fpCreateWindowExA(dwExStyle, lpClassName, lpWindowName, dwStyle, X, Y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
    HideWindow(hwnd);
    return hwnd;
}

typedef HWND(*CreateWindowExWType)(
    DWORD     dwExStyle,
    LPCWSTR    lpClassName,
    LPCWSTR    lpWindowName,
    DWORD     dwStyle,
    int       X,
    int       Y,
    int       nWidth,
    int       nHeight,
    HWND      hWndParent,
    HMENU     hMenu,
    HINSTANCE hInstance,
    LPVOID    lpParam
    );

CreateWindowExWType fpCreateWindowExW = NULL;

HWND DetourCreateWindowExW(
    DWORD     dwExStyle,
    LPCWSTR    lpClassName,
    LPCWSTR    lpWindowName,
    DWORD     dwStyle,
    int       X,
    int       Y,
    int       nWidth,
    int       nHeight,
    HWND      hWndParent,
    HMENU     hMenu,
    HINSTANCE hInstance,
    LPVOID    lpParam
) {
    HWND hwnd = fpCreateWindowExW(dwExStyle, lpClassName, lpWindowName, dwStyle, X, Y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
    HideWindow(hwnd);
    return hwnd;
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        HideAllProcessWindows(GetCurrentProcessId());
        MH_Initialize();
        MH_CreateHookApi(L"user32", "CreateWindowExA", &DetourCreateWindowExA, (LPVOID*)&fpCreateWindowExA);
        MH_EnableHook(&CreateWindowExA);
        MH_CreateHookApi(L"user32", "CreateWindowExW", &DetourCreateWindowExW, (LPVOID*)&fpCreateWindowExW);
        MH_EnableHook(&CreateWindowExW);
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

