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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Controls;
using WPFLocalizeExtension.Engine;
using static Vanara.PInvoke.User32;

namespace HideMyWindows.App.Services
{
    public class WindowRulesMatcherService : IHostedService
    {
        private IProcessWatcher ProcessWatcher { get; }
        private IWindowWatcher WindowWatcher { get; }
        private IConfigProvider ConfigProvider { get; }
        private IWindowHider WindowHider { get; }
        private ISnackbarService SnackbarService { get; }

        public WindowRulesMatcherService(IProcessWatcher processWatcher, IWindowWatcher windowWatcher, IConfigProvider configProvider, IWindowHider windowHider, ISnackbarService snackbarService)
        {
            ProcessWatcher = processWatcher;
            WindowWatcher = windowWatcher;
            ConfigProvider = configProvider;
            WindowHider = windowHider;
            SnackbarService = snackbarService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            ProcessWatcher.ProcessStarted += OnProcessStarted;
            WindowWatcher.WindowCreated += OnWindowCreated;

            _ = Task.Run(async () =>
            {
                var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250)); // Tick every 250ms
                long lastTick = 0;
                
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    if (ConfigProvider.Config is null) continue;

                    // Simple throttling based on config
                    var interval = ConfigProvider.Config.RuleReapplyIntervalMs;
                    var now = Environment.TickCount64;
                    if (now - lastTick < interval) continue;
                    
                    lastTick = now;
                    
                    try 
                    {
                        await ReapplyPersistentRules();
                    }
                    catch { }
                }
            }, cancellationToken);
        }

        private async Task ReapplyPersistentRules()
        {
            if (ConfigProvider.Config is null) return;
            
            var persistentRules = ConfigProvider.Config.WindowRules.Where(rule => rule.Enabled && rule.Persistent).ToList();
            if (!persistentRules.Any()) return;

            // TODO: Respect Config.RuleReapplyIntervalMs (currently hardcoded 100ms loop effectively, need throttling)
            // For now let's just use the timer tick.
            
            EnumWindows((hwnd, lParam) =>
            {
                // Verify window is valid
                if (!IsWindow(hwnd) || !IsWindowVisible(hwnd)) return true; // Visible check? Persistent rules might want to KEEP hidden so we process hidden too? 
                // Actually if we want to UNHIDE, we need to process non-visible windows too.
                // But IsWindowVisible(hwnd) returns false if hidden.
                // If we want to HIDE, we target visible.
                // If we want to UNHIDE, we target hidden.
                // So let's just process all top level windows.
                
                // optimization: cache process name/id
                
                int? processId = null;
                string? processName = null;
                
                // Get window title/class
                var length = GetWindowTextLength(hwnd);
                var sb = new StringBuilder(length + 1);
                GetWindowText(hwnd, sb, length + 1);
                var title = sb.ToString();
                
                sb = new StringBuilder(256);
                GetClassName(hwnd, sb, 256);
                var className = sb.ToString();

                foreach (var rule in persistentRules)
                {
                   string value = string.Empty;

                    if (rule.Target == WindowRuleTarget.WindowTitle)
                    {
                        value = title;
                    }
                    else if (rule.Target == WindowRuleTarget.WindowClass)
                    {
                        value = className;
                    }
                    else if (rule.Target == WindowRuleTarget.ProcessName || rule.Target == WindowRuleTarget.ProcessId)
                    {
                        if (processId == null)
                        {
                            GetWindowThreadProcessId(hwnd, out var pid);
                            processId = (int)pid;
                        }

                        if (rule.Target == WindowRuleTarget.ProcessId)
                        {
                            value = processId.ToString()!;
                        }
                        else if (rule.Target == WindowRuleTarget.ProcessName)
                        {
                            if (processName == null)
                            {
                                try
                                {
                                    using var process = Process.GetProcessById(processId.Value);
                                    processName = process.ProcessName;
                                }
                                catch
                                {
                                    processName = string.Empty;
                                }
                            }
                            value = processName;
                        }
                    }

                    if (rule.Matches(value))
                    {
                        try 
                        {
                             // Re-apply!
                             WindowHider.ApplyAction(rule.Action, (IntPtr)hwnd);
                        }
                        catch {}
                    }
                }
                
                return true;
            }, IntPtr.Zero);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        private void OnWindowCreated(object? sender, WindowWatchedEventArgs e)
        {
            if (ConfigProvider.Config is not null)
            {
                var rules = ConfigProvider.Config.WindowRules.Where(rule => rule.Enabled);
                string? processName = null;
                int? processId = null;

                foreach (var rule in rules)
                {
                    string value = string.Empty;

                    if (rule.Target == WindowRuleTarget.WindowTitle)
                    {
                        value = e.Title;
                    }
                    else if (rule.Target == WindowRuleTarget.WindowClass)
                    {
                        value = e.Class;
                    }
                    else if (rule.Target == WindowRuleTarget.ProcessName || rule.Target == WindowRuleTarget.ProcessId)
                    {
                        // Resolve process info lazily
                        if (processId == null)
                        {
                            GetWindowThreadProcessId(e.Handle, out var pid);
                            processId = (int)pid;
                        }

                        if (rule.Target == WindowRuleTarget.ProcessId)
                        {
                            value = processId.ToString()!;
                        }
                        else if (rule.Target == WindowRuleTarget.ProcessName)
                        {
                            if (processName == null)
                            {
                                try
                                {
                                    using var process = Process.GetProcessById(processId.Value);
                                    processName = process.ProcessName; 
                                    // Note: ProcessName usually doesn't include .exe. 
                                    // If rules expect .exe, we might need verify how they are created.
                                    // Helper 'TryGetProcessNameWithExtension' might be better if available/public.
                                    // For now using ProcessName as per standard .NET
                                }
                                catch
                                {
                                    processName = string.Empty;
                                }
                            }
                            value = processName;
                        }
                    }

                    if (rule.Matches(value))
                    {
                        try
                        {
                            WindowHider.ApplyAction(rule.Action, e.Handle);
                        }
                        catch (ArgumentException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            SnackbarService.Show(LocalizationUtils.GetString("AnErrorOccurred"), ex.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24));
                        }
                    }
                }
            }
        }

        private void OnProcessStarted(object? sender, ProcessWatchedEventArgs e)
        {
            if (ConfigProvider.Config is not null)
            {
                var rules = ConfigProvider.Config.WindowRules.Where(rule => rule.Enabled && rule.Target is WindowRuleTarget.ProcessName or WindowRuleTarget.ProcessId);
                foreach (var rule in rules)
                {
                    var value = rule.Target switch
                    {
                        WindowRuleTarget.ProcessName => e.Name,
                        WindowRuleTarget.ProcessId => e.Id.ToString(),
                        _ => throw new NotImplementedException(),
                    };


                    if (rule.Matches(value))
                    {
                        try
                        {
                            var process = Process.GetProcessById(e.Id);

                            WindowHider.ApplyAction(rule.Action, process);
                        }
                        catch (ArgumentException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            SnackbarService.Show(LocalizationUtils.GetString("AnErrorOccurred"), ex.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24));
                        }
                    }
                }
            }
        }
    }
}
