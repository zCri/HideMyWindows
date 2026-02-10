#pragma once
#include <windows.h>
#include <wtypes.h>
#include <string>
#include <format>

// Hooks
void HmwSetupHooks();

typedef BOOL(WINAPI* SetWindowDisplayAffinityType)(
    _In_ HWND  hWnd,
    _In_ DWORD dwAffinity
);
extern SetWindowDisplayAffinityType fpSetWindowDisplayAffinity;

// Mailslot
void HmwEnsureMailslot();

enum HmwMailslotMessageType {
    LOG = 2,
};
void HmwSendMailslotMessage(HmwMailslotMessageType type, std::wstring msg);

// Hider
extern bool isActivelyHiding;
void HmwSetWindowVisibility(HWND hwnd, bool visible);
void HmwSetAllWindowsVisibility(bool visible);
void HmwSetTrayIconVisibility(HWND hwnd, bool visibile);

// Utils
LPWSTR GetLastErrorAsString();

enum HmwLogLevel {
	INFO,
	WARN,
	ERR,
};

void _HmwLog(HmwLogLevel level, std::wstring message);
template <typename... Args>
void HmwLog(HmwLogLevel level, std::wformat_string<Args...> fmt, Args&&... args) {
    std::wstring finalMessage = std::format(fmt, std::forward<Args>(args)...);
    _HmwLog(level, finalMessage);
}

#define HmwError(fmt, ...) HmwLog(HmwLogLevel::ERR, fmt, __VA_ARGS__)
