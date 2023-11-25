using HideMyWindows.App.Models;
using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Services.DllInjector;
using HideMyWindows.App.Services.ProcessWatcher;
using HideMyWindows.App.Services.WindowWatcher;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using static Vanara.PInvoke.User32;

namespace HideMyWindows.App.Services
{
    public class WindowRulesMatcherService : IHostedService
    {
        private IProcessWatcher ProcessWatcher { get; }
        private IWindowWatcher WindowWatcher { get; }
        private IConfigProvider ConfigProvider { get; }
        private IDllInjector DllInjector { get; }
        private ISnackbarService SnackbarService { get; }

        public WindowRulesMatcherService(IProcessWatcher processWatcher, IWindowWatcher windowWatcher, IConfigProvider configProvider, IDllInjector dllInjector, ISnackbarService snackbarService)
        {
            ProcessWatcher = processWatcher;
            WindowWatcher = windowWatcher;
            ConfigProvider = configProvider;
            DllInjector = dllInjector;
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
                            GetWindowThreadProcessId(e.Handle, out var pid);
                            var process = Process.GetProcessById((int) pid);

                            DllInjector.InjectDll(process);
                        }
                        catch (ArgumentException)
                        {
                            return;
                        }
                        catch (Exception ex) // TODO: Handle errors
                        {
                            SnackbarService.Show("An error occurred!", ex.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24));
                        }
                    }
                }
            }
        }

        private void OnProcessStarted(object? sender, ProcessWatchedEventArgs e)
        {
            if (ConfigProvider.Config is not null)
            {
                var rules = ConfigProvider.Config.WindowRules.Where(rule => rule.Enabled && rule.Target is WindowRuleTarget.ProcessName);
                foreach (var rule in rules)
                {
                    var value = e.Name;
                    
                    if (rule.Matches(value))
                    {
                        try
                        {
                            var process = Process.GetProcessById(e.Id);

                            DllInjector.InjectDll(process);
                        }
                        catch (ArgumentException)
                        {
                            return;
                        }
                        catch (Exception) // TODO: Handle errors
                        {

                        }
                    }
                }
            }
        }
    }
}
