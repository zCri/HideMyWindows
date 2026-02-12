using HideMyWindows.App.Helpers;
using HideMyWindows.App.Models;
using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Services.DllInjector;
using HideMyWindows.App.Services.ProcessWatcher;
using HideMyWindows.App.Services.WindowHider;
using HideMyWindows.App.Services.WindowWatcher;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wpf.Ui.Controls;
using WPFLocalizeExtension.Engine;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Kernel32;
using Vanara.PInvoke;

namespace HideMyWindows.App.Services
{
    public class WindowRulesMatcherService : IHostedService
    {
        private IProcessWatcher ProcessWatcher { get; }
        private IWindowWatcher WindowWatcher { get; }
        private IConfigProvider ConfigProvider { get; }
        private IWindowHider WindowHider { get; }
        private ISnackbarService SnackbarService { get; }

        public WindowRulesMatcherService(
            IProcessWatcher processWatcher,
            IWindowWatcher windowWatcher,
            IConfigProvider configProvider,
            IWindowHider windowHider,
            ISnackbarService snackbarService)
        {
            ProcessWatcher = processWatcher;
            WindowWatcher = windowWatcher;
            ConfigProvider = configProvider;
            WindowHider = windowHider;
            SnackbarService = snackbarService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ProcessWatcher.ProcessStarted += OnProcessStarted;
            WindowWatcher.WindowCreated += OnWindowCreated;

            _ = Task.Run(async () => await RunPersistentRuleLoop(cancellationToken), cancellationToken);

            await Task.CompletedTask;
        }

        private async Task RunPersistentRuleLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int delay = ConfigProvider.Config?.RuleReapplyIntervalMs ?? 1000;

                try
                {
                    ReapplyPersistentRules();
                }
                catch (Exception ex)
                {
                    // Log to debug only
                    Debug.WriteLine($"[WindowRulesMatcher] Loop Error: {ex.Message}");
                }

                await Task.Delay(delay, cancellationToken);
            }
        }

        private void ReapplyPersistentRules()
        {
            if (ConfigProvider.Config is null) return;

            var persistentRules = ConfigProvider.Config.WindowRules
                .Where(rule => rule.Enabled && rule.Persistent);

            if (!persistentRules.Any()) return;
            EnumWindows((hwnd, lParam) =>
            {
                if (!IsWindow(hwnd) || !IsWindowVisible(hwnd)) return true;

                CheckAndApplyRulesForWindow(persistentRules, hwnd);

                return true;
            }, IntPtr.Zero);
        }

        private void OnWindowCreated(object? sender, WindowWatchedEventArgs e)
        {
            if (ConfigProvider.Config is null) return;

            var activeRules = ConfigProvider.Config.WindowRules
                .Where(rule => rule.Enabled);

            CheckAndApplyRulesForWindow(activeRules, e.Handle, e.Title, e.Class);
        }

        private void CheckAndApplyRulesForWindow(IEnumerable<WindowRule> rules, HWND hwnd, string? windowTitle = null, string? windowClass = null)
        {
            int? processId = null;
            string? processName = null;

            foreach (var rule in rules)
            {
                string valueToCheck = string.Empty;

                try
                {
                    switch (rule.Target)
                    {
                        case WindowRuleTarget.WindowTitle:
                            windowTitle ??= GetWindowTitle(hwnd);
                            valueToCheck = windowTitle;
                            break;

                        case WindowRuleTarget.WindowClass:
                            windowClass ??= GetWindowClassName(hwnd);
                            valueToCheck = windowClass;
                            break;

                        case WindowRuleTarget.ProcessId:
                            processId ??= GetWindowProcessId(hwnd);
                            valueToCheck = processId.ToString()!;
                            break;

                        case WindowRuleTarget.ProcessName:
                            processId ??= GetWindowProcessId(hwnd);
                            processName ??= GetProcessNameFromPid(processId.Value);
                            valueToCheck = processName;
                            break;
                    }

                    if (!string.IsNullOrEmpty(valueToCheck) && rule.Matches(valueToCheck))
                    {
                        WindowHider.ApplyAction(rule.Action, hwnd.DangerousGetHandle());
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        private void OnProcessStarted(object? sender, ProcessWatchedEventArgs e)
        {
            if (ConfigProvider.Config is null) return;

            var rules = ConfigProvider.Config.WindowRules
                .Where(rule => rule.Enabled && (rule.Target == WindowRuleTarget.ProcessName || rule.Target == WindowRuleTarget.ProcessId));

            foreach (var rule in rules)
            {
                var value = rule.Target switch
                {
                    WindowRuleTarget.ProcessName => e.Name,
                    WindowRuleTarget.ProcessId => e.Id.ToString(),
                    _ => string.Empty
                };

                if (rule.Matches(value))
                {
                    try
                    {
                        using var process = Process.GetProcessById(e.Id);
                        WindowHider.ApplyAction(rule.Action, process);
                    }
                    catch (ArgumentException) { /* Process likely exited immediately */ }
                    catch (Exception ex)
                    {
                        SnackbarService.Show(
                            LocalizationUtils.GetString("AnErrorOccurred"),
                            ex.Message,
                            ControlAppearance.Danger,
                            new SymbolIcon(SymbolRegular.ErrorCircle24));
                    }
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ProcessWatcher.ProcessStarted -= OnProcessStarted;
            WindowWatcher.WindowCreated -= OnWindowCreated;
            await Task.CompletedTask;
        }

        private string GetWindowTitle(HWND hwnd)
        {
            var length = GetWindowTextLength(hwnd);
            if (length == 0) return string.Empty;

            var sb = new StringBuilder(length + 1);
            GetWindowText(hwnd, sb, length + 1);
            return sb.ToString();
        }

        private string GetWindowClassName(HWND hwnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hwnd, sb, 256);
            return sb.ToString();
        }

        private int GetWindowProcessId(HWND hwnd)
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            return (int)pid;
        }

        private string GetProcessNameFromPid(int pid)
        {
            // PROCESS_QUERY_LIMITED_INFORMATION (0x1000) is sufficient for Windows Vista+
            // and allows accessing system processes that standard Access Rights might block.
            using var hProcess = OpenProcess(ACCESS_MASK.FromEnum(ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION | ProcessAccess.PROCESS_VM_READ), false, (uint)pid);

            if (hProcess.IsInvalid) return string.Empty;
            var sb = new StringBuilder(1024);
            // GetModuleFileNameEx works better for 32/64 bit interop than GetModuleBaseName in some cases,
            // providing the full path. We then extract the filename.
            // Note: Vanara usually exposes this via Psapi or Kernel32. 
            // Using a fallback safe definition if Vanara structure is complex.
            uint size = (uint)sb.Capacity;
            
            // Assuming Vanara.PInvoke.Psapi is available or Kernel32 for QueryFullProcessImageName
            // We will try QueryFullProcessImageName as it is the modern replacement
            if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
            {
                return Path.GetFileName(sb.ToString());
            }

            return string.Empty;
        }
    }
}