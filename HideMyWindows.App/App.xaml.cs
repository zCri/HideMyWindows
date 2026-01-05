// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Services;
using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Services.DesktopPreview;
using HideMyWindows.App.Services.DllInjector;
using HideMyWindows.App.Services.ProcessWatcher;
using HideMyWindows.App.Services.TourService;
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
using Vanara.PInvoke;
using Wpf.Ui.DependencyInjection;
using WPFLocalizeExtension.Engine;
using static Vanara.PInvoke.Kernel32;

namespace HideMyWindows.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, "crash_log_domain.txt");
                File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + (e.ExceptionObject as Exception)?.ToString() + Environment.NewLine);
                MessageBox.Show($"A critical error occurred: {(e.ExceptionObject as Exception)?.Message}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        public static readonly Queue<SimpleContentDialogCreateOptions> DeferredContentDialogs = new();

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
                services.AddNavigationViewPageProvider();
                services.AddHostedService<ApplicationHostService>();

                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();
                services.AddSingleton<ITourService, TourService>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<WindowRulesPage>();
                services.AddSingleton<WindowRulesViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<QuickLaunchPage>();
                services.AddSingleton<QuickLaunchViewModel>();
                services.AddSingleton<DesktopPreviewPage>();
                services.AddSingleton<DesktopPreviewViewModel>();

                // App services
                services.AddSingleton<IConfigProvider, JSONConfigProvider>();
                services.AddSingleton<IDllInjector, LoadLibraryDllInjector>();
                services.AddSingleton<IWindowHider, DllInjectorWindowHider>();
                services.AddSingleton<IDesktopPreviewService, D3DCaptureDesktopPreviewService>();
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

                services.AddSingleton<IWindowWatcher>((serviceProvider) =>
                {
                    var configProvider = serviceProvider.GetRequiredService<IConfigProvider>();
                    configProvider.Load();

                    return configProvider.Config!.WindowWatcherType switch
                    {
                        WindowWatcherType.UIAutomationWindowWatcher => (IWindowWatcher)ActivatorUtilities.CreateInstance(serviceProvider, typeof(UIAutomationWindowWatcher)),
                        WindowWatcherType.ShellHookWindowWatcher => (IWindowWatcher)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ShellHookWindowWatcher)),
                        _ => throw new NotImplementedException()
                    };
                });
                
                services.AddHostedService<WindowRulesMatcherService>();
                services.AddHostedService<MailslotIPCService>();
                //TODO: Arm support? (late)
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
            var currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            if (currentDirectory is not null)
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }

            LocalizeDictionary.Instance.Culture = CultureInfo.CurrentUICulture;

            var hMailslot = CreateFile(@"\\.\mailslot\HideMyWindowsMailslot", Vanara.PInvoke.Kernel32.FileAccess.GENERIC_WRITE, 0, null, FileMode.Open, 0, HFILE.NULL);
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
            
            var logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, "crash_log.txt");
            File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + e.Exception.ToString() + Environment.NewLine);
            
            MessageBox.Show($"An unhandled exception occurred: {e.Exception.Message}\nCheck crash_log.txt for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
