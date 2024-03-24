// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Services;
using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Services.DllInjector;
using HideMyWindows.App.Services.ProcessWatcher;
using HideMyWindows.App.Services.WindowClickFinder;
using HideMyWindows.App.Services.WindowHider;
using HideMyWindows.App.Services.WindowWatcher;
using HideMyWindows.App.ViewModels.Pages;
using HideMyWindows.App.ViewModels.Windows;
using HideMyWindows.App.Views.Pages;
using HideMyWindows.App.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Threading;
using WPFLocalizeExtension.Engine;
using static Vanara.PInvoke.Kernel32;

namespace HideMyWindows.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {

        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location) ?? string.Empty); })
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<ApplicationHostService>();

                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<WindowRulesPage>();
                services.AddSingleton<WindowRulesViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<QuickLaunchPage>();
                services.AddSingleton<QuickLaunchViewModel>();

                // App services
                services.AddSingleton<IConfigProvider, JSONConfigProvider>();
                services.AddSingleton<IDllInjector, LoadLibraryDllInjector>();
                services.AddSingleton<IWindowHider, DllInjectorWindowHider>();
                services.AddSingleton<NotificationsService>();

                services.AddTransient<IWindowClickFinder, MouseCaptureWindowClickFinder>();

                // Really ugly way to do dynamic dependency type resolution
                services.AddSingleton<IProcessWatcher>((serviceProvider) =>
                {
                    var configProvider = serviceProvider.GetRequiredService<IConfigProvider>();
                    configProvider.Load();

                    return configProvider.Config!.ProcessWatcherType switch
                    {
                        ProcessWatcherType.WMIInstanceEventProcessWatcher => (IProcessWatcher)ActivatorUtilities.CreateInstance(serviceProvider, typeof(WMIInstanceEventProcessWatcher)),
                        ProcessWatcherType.WMIProcessTraceProcessWatcher => (IProcessWatcher)ActivatorUtilities.CreateInstance(serviceProvider, typeof(WMIProcessTraceProcessWatcher)),
                        _ => throw new NotImplementedException()
                    };
                });

                //TODO: Win32 hooks ?
                services.AddSingleton<IWindowWatcher>((serviceProvider) =>
                {
                    var configProvider = serviceProvider.GetRequiredService<IConfigProvider>();
                    configProvider.Load();

                    return configProvider.Config!.WindowWatcherType switch
                    {
                        WindowWatcherType.UIAutomationWindowWatcher => (IWindowWatcher)ActivatorUtilities.CreateInstance(serviceProvider, typeof(UIAutomationWindowWatcher)),
                        _ => throw new NotImplementedException()
                    };
                });
                
                //TODO: Fix MSBuild
                services.AddHostedService<WindowRulesMatcherService>();
                services.AddHostedService<MailslotIPCService>();
            }).Build();

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T? GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            LocalizeDictionary.Instance.Culture = CultureInfo.CurrentUICulture;
            var hMailslot = CreateFile(@"\\.\mailslot\HideMyWindowsMailslot", Vanara.PInvoke.Kernel32.FileAccess.GENERIC_WRITE, 0, null, FileMode.Open, 0, null);
            if (!hMailslot.IsInvalid)
            {
                var bytes = Encoding.Unicode.GetBytes("0");
                WriteFile(hMailslot, bytes, (uint)bytes.Length, out _, IntPtr.Zero);
                Shutdown();
            }
            else
            {
                _host.Start();
            }
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
