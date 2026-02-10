#include "HideMyWindows.h"

HANDLE hMailslot = INVALID_HANDLE_VALUE;

void HmwEnsureMailslot() {
    bool handleDead = false;

    if (hMailslot == NULL || hMailslot == INVALID_HANDLE_VALUE) {
        handleDead = true;
    }
    else {
        DWORD nextMsg, msgCount;
        if (!GetMailslotInfo(hMailslot, NULL, &nextMsg, &msgCount, NULL)) {
            CloseHandle(hMailslot);
            hMailslot = INVALID_HANDLE_VALUE;
            handleDead = true;
        }
    }

    if (handleDead) {
        hMailslot = CreateFile(
            L"\\\\.\\mailslot\\HideMyWindowsMailslot",
            GENERIC_WRITE,
            FILE_SHARE_READ,
            NULL,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            NULL
        );
    }
}

void HmwSendMailslotMessage(HmwMailslotMessageType type, std::wstring msg) {
    HmwEnsureMailslot();
    
    std::wstring formattedMsg = std::format(L"{}|{}", static_cast<int>(type), msg); 
    DWORD cbWritten;
    WriteFile(
        hMailslot,
        formattedMsg.c_str(),
        formattedMsg.size() * sizeof(WCHAR),
        &cbWritten,
        NULL
    );
}