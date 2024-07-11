// NOTE: When changing only the dll, need to rebuild it for both architectures because Visual Studio will not build both automatically.
#include <windows.h>
#include <tlhelp32.h>
#include <stdio.h>
#include <MinHook.h>
#include <iostream>  
#include <string>  
using namespace std;

bool hideAllWindows = false;
bool followChildProcesses = false;

HANDLE hMailslot;

LPSTR GetLastErrorAsString()
{
    //Get the error message ID, if any.
    DWORD errorMessageID = ::GetLastError();

    LPSTR messageBuffer = nullptr;

    //Ask Win32 to give us the string version of that message ID.
    //The parameters we pass in, tell Win32 to create the buffer that holds the message for us (because we don't yet know how long the message string will be).
    size_t size = FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL, errorMessageID, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPSTR)&messageBuffer, 0, NULL);

    return messageBuffer;
}

void _HideWindow(HWND hwnd) {
    //TODO: doesnt work for createwindow hook ?
    if (!SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE)) {
        //MessageBoxA(NULL, GetLastErrorAsString(), "Debug", MB_OK);
    }
}

// Existing windows
BOOL CALLBACK EnumThreadWndProc(
    HWND   hwnd,
    LPARAM lParam
) {
    //MessageBoxA(NULL, std::to_string((long)hwnd).c_str(), "enumwindows", MB_OK);
    if(hideAllWindows)
        _HideWindow(hwnd);
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

void ConnectToMailslot() {
    hMailslot = CreateFile(
        L"\\\\.\\mailslot\\HideMyWindowsMailslot",
        GENERIC_WRITE,
        0,
        NULL,
        OPEN_EXISTING,
        0,
        NULL
    );

    if (hMailslot == INVALID_HANDLE_VALUE) {
        //MessageBoxA(NULL, GetLastErrorAsString(), "Debug", MB_OK);
    }
}

void SendPid(int pid) {
    //MessageBox(NULL, L"Sending pid", L"Debug", MB_OK);
    TCHAR buf[128];
    int len = swprintf_s(buf, 127, L"1|%d", pid);
    if (len == -1) {
        //MessageBox(NULL, L"error swprintf", L"Debug", MB_OK);
        //MessageBoxA(NULL, std::to_string(pid).c_str(), "pid = ", MB_OK);
        return;
    }

    DWORD cbWritten;
    if (FALSE == WriteFile(
        hMailslot,
        buf,
        (len + 1) * sizeof(TCHAR),
        &cbWritten,
        NULL
    )) {
        //MessageBox(NULL, L"writefile error", L"Debug", MB_OK);
        //MessageBoxA(NULL, GetLastErrorAsString(), "Debug", MB_OK);
        return;
    }
    //MessageBox(NULL, L"Sent pid to pipe", L"Debug", MB_OK);
}

void OnProcessCreated(DWORD dwProcessId) {
    //MessageBox(NULL, L"OnProcessCreated", L"Debug", MB_OK);
    if (followChildProcesses) {
        SendPid(dwProcessId);
    }
}

// New windows
typedef HWND(WINAPI *CreateWindowExAType)(
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

HWND WINAPI DetourCreateWindowExA(
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
    if(hideAllWindows)
        _HideWindow(hwnd);
    return hwnd;
}

typedef HWND(WINAPI *CreateWindowExWType)(
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

HWND WINAPI DetourCreateWindowExW(
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
    if (hideAllWindows)
        _HideWindow(hwnd);
    return hwnd;
}

typedef BOOL (WINAPI *CreateProcessAType)(
    LPCSTR                lpApplicationName,
    LPSTR                 lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    BOOL                  bInheritHandles,
    DWORD                 dwCreationFlags,
    LPVOID                lpEnvironment,
    LPCSTR                lpCurrentDirectory,
    LPSTARTUPINFOA        lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
);

CreateProcessAType fpCreateProcessA = NULL;

BOOL WINAPI DetourCreateProcessA(
    LPCSTR                lpApplicationName,
    LPSTR                 lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    BOOL                  bInheritHandles,
    DWORD                 dwCreationFlags,
    LPVOID                lpEnvironment,
    LPCSTR                lpCurrentDirectory,
    LPSTARTUPINFOA        lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
) {
    //MessageBox(NULL, L"Calling createa", L"Debug", MB_OK);
    BOOL res = fpCreateProcessA(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation);
    if (res) {
        
        //MessageBox(NULL, L"Called createa + res", L"Debug", MB_OK);
        OnProcessCreated(lpProcessInformation->dwProcessId);
    }

    return res;
}

typedef BOOL(WINAPI* CreateProcessWType)(
    LPCWSTR                lpApplicationName,
    LPWSTR                 lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    BOOL                  bInheritHandles,
    DWORD                 dwCreationFlags,
    LPVOID                lpEnvironment,
    LPCWSTR                lpCurrentDirectory,
    LPSTARTUPINFOW        lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
);

CreateProcessWType fpCreateProcessW = NULL;

BOOL WINAPI DetourCreateProcessW(
    LPCWSTR                lpApplicationName,
    LPWSTR                 lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    BOOL                  bInheritHandles,
    DWORD                 dwCreationFlags,
    LPVOID                lpEnvironment,
    LPCWSTR                lpCurrentDirectory,
    LPSTARTUPINFOW        lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
) {
    //MessageBox(NULL, L"Calling createw", L"Debug", MB_OK);
    BOOL res = fpCreateProcessW(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation);
    if (res) {
        //MessageBox(NULL, L"Called createa + res", L"Debug", MB_OK);
        OnProcessCreated(lpProcessInformation->dwProcessId);
    }

    return res;
}

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        //HideAllProcessWindows(GetCurrentProcessId());
        ConnectToMailslot();
        MH_Initialize();
        MH_CreateHookApi(L"user32.dll", "CreateWindowExA", &DetourCreateWindowExA, (LPVOID*)&fpCreateWindowExA);
        MH_EnableHook(&CreateWindowExA);
        MH_CreateHookApi(L"user32.dll", "CreateWindowExW", &DetourCreateWindowExW, (LPVOID*)&fpCreateWindowExW);
        MH_EnableHook(&CreateWindowExW);
        MH_CreateHookApi(L"kernel32.dll", "CreateProcessA", &DetourCreateProcessA, (LPVOID*)&fpCreateProcessA);
        MH_EnableHook(&CreateProcessA);
        MH_CreateHookApi(L"kernel32.dll", "CreateProcessW", &DetourCreateProcessW, (LPVOID*)&fpCreateProcessW);
        MH_EnableHook(&CreateProcessW);
        //MessageBoxA(NULL, std::to_string(MH_CreateHookApi(L"kernel32.dll", "CreateProcessW", &DetourCreateProcessW, (LPVOID*)&fpCreateProcessW)).c_str(), "Createprocessw:", MB_OK);
        //MessageBoxA(NULL, std::to_string(MH_EnableHook(&CreateProcessW)).c_str(), "Createprocessw enable:", MB_OK);
        //MessageBox(NULL, L"Hooked everything", L"Debug", MB_OK);
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

struct HideWindowParameter
{
    HWND hwnd;
};

extern "C" __declspec(dllexport) void HideWindow(HideWindowParameter* param) {
    _HideWindow(param->hwnd);
}

extern "C" __declspec(dllexport) void HideAllWindows() {
    hideAllWindows = true;
    followChildProcesses = true;
    HideAllProcessWindows(GetCurrentProcessId());
}