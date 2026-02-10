#include "HideMyWindows.h"
#include <ShObjIdl.h>

// Window
void HmwSetWindowVisibility(HWND hwnd, bool visible) {
    if (!fpSetWindowDisplayAffinity(hwnd, visible ? WDA_NONE : WDA_EXCLUDEFROMCAPTURE)) {
        HmwError(L"Could not set window display affinity: {}", GetLastErrorAsString());
    }
}

// All windows
DWORD ownPid;
BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam) {
    bool visible = (bool)lParam;

    DWORD pid;
    GetWindowThreadProcessId(hwnd, &pid);
    if (pid == ownPid && IsWindowVisible(hwnd)) { // Checking IsWindowVisible to avoid white window bug
        HmwSetWindowVisibility(hwnd, visible);
    }

    return TRUE;
}

bool isActivelyHiding = false;
void HmwSetAllWindowsVisibility(bool visible) {
    isActivelyHiding = !visible;
    ownPid = GetCurrentProcessId();
    EnumWindows(EnumWindowsProc, (LPARAM)visible);
}

// Tray icon (thanks to minhprovjp)
void HmwSetTrayIconVisibility(HWND hwnd, bool visibile) {
    HRESULT hr;
    ITaskbarList* pTaskbarList = NULL;
    bool coInitialized = false;

    hr = CoInitialize(NULL);
    if (SUCCEEDED(hr)) {
        coInitialized = true;
    }
    else if (hr == RPC_E_CHANGED_MODE) {
        // COM already initialized with a different mode, we can proceed but shouldn't uninitialize
        hr = S_OK;
    }
    else {
        // Any other failure
        return;
    }

    hr = CoCreateInstance(CLSID_TaskbarList, NULL, CLSCTX_INPROC_SERVER, IID_ITaskbarList, (void**)&pTaskbarList);
    if (SUCCEEDED(hr)) {
        pTaskbarList->HrInit();
        if (visibile) {
            pTaskbarList->AddTab(hwnd);
        }
        else {
            pTaskbarList->DeleteTab(hwnd);
        }
        pTaskbarList->Release();
    }

    if (coInitialized) {
        CoUninitialize();
    }
}