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
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        private void OnWindowCreated(object? sender, WindowWatchedEventArgs e)
        {
            if (ConfigProvider.Config is not null)
            {
                var rules = ConfigProvider.Config.WindowRules.Where(rule => rule.Enabled && rule.Target is WindowRuleTarget.WindowTitle or WindowRuleTarget.WindowTitle);
                foreach (var rule in rules)
                {
                    var value = rule.Target switch
                    {
                        WindowRuleTarget.WindowTitle => e.Title,
                        WindowRuleTarget.WindowClass => e.Class,
                        _ => string.Empty
                    };

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
