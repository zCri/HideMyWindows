// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Services.TourService;
using HideMyWindows.App.Services.WindowWatcher;
using HideMyWindows.App.Views.Pages;
using HideMyWindows.App.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Windows.Interop;
using Wpf.Ui.Appearance;
using WPFLocalizeExtension.Engine;
using static Vanara.PInvoke.User32;

namespace HideMyWindows.App.Services
{
    /// <summary>
    /// Managed host of the application.
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public ApplicationHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private async Task HandleActivationAsync()
        {
            await Task.CompletedTask;

            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                var configProvider = _serviceProvider.GetService<IConfigProvider>();
                configProvider?.Load();

                if (configProvider?.Config!.SelectedCulture is not null) CultureInfo.CurrentUICulture = configProvider.Config.SelectedCulture;
                LocalizeDictionary.Instance.Culture = CultureInfo.CurrentUICulture; 
                var navigationWindow = (
                    _serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow
                )!;
                
                navigationWindow!.ShowWindow();

                navigationWindow.Navigate(typeof(DashboardPage));

                // Weird behaviour with the JSON deserialization (?), not calling the property setter if the value in the config is equal to the default, might be fixable with JsonSerializerOptions.PreferredObjectCreationHandling (?) but it's .NET 8+.
                // Just a quick hack before i actually decide what to do with it, also causes the configuration options to apply twice if they are not the default values.
                if (configProvider is not null && configProvider.Config is not null)
                {

                }

                foreach (var _window in Application.Current.Windows)
                {
                    var window = _window as Window;
                    if (window is null) continue;

                    var interop = new WindowInteropHelper(window);
                    
                    var hwnd = interop.EnsureHandle();

                    if (window == Application.Current.MainWindow)
                    {
                        if (_serviceProvider.GetRequiredService(typeof(IWindowWatcher)) is ShellHookWindowWatcher windowWatcher)
                        {
                            windowWatcher.Initialize(hwnd);
                        }

                        window.StateChanged += (sender, args) => {
                            if (window.WindowState == WindowState.Minimized && configProvider!.Config!.MinimizeToTrayIcon) window.Hide();
                        };
                    }

                    SetWindowDisplayAffinity(hwnd, configProvider?.Config?.HideSelf ?? true ? (WindowDisplayAffinity)0x11 : WindowDisplayAffinity.WDA_NONE);
                }

                ApplicationThemeManager.Apply(configProvider?.Config?.CurrentTheme ?? ApplicationThemeManager.GetAppTheme());

                _serviceProvider.GetService<ITourService>()?.TryAutoStart();
            }
        }
    }
}
