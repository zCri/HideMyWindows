// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Controls;
using HideMyWindows.App.Helpers;
using HideMyWindows.App.Services;
using HideMyWindows.App.Services.ConfigProvider;
using System.Collections.ObjectModel;
using Vanara.PInvoke;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using WPFLocalizeExtension.Engine;
using static Vanara.PInvoke.Kernel32;

namespace HideMyWindows.App.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public IConfigProvider ConfigProvider { get; }
        public IContentDialogService ContentDialogService { get; }
        public NotificationsService NotificationsService { get; }

        public MainWindowViewModel(IConfigProvider configProvider, NotificationsService notificationsService, IContentDialogService contentDialogService)
        {
            ConfigProvider = configProvider;
            NotificationsService = notificationsService;
            ContentDialogService = contentDialogService;
        }

        public async void Initialize()
        {
            ConfigProvider.Load();
            
            OSVERSIONINFOEX osVersionInfo = OSVERSIONINFOEX.Default;
            GetVersionEx(ref osVersionInfo);
            if (osVersionInfo.dwBuildNumber < 19041 && !ConfigProvider.Config!.VersionWarningAcknowledged) // Windows 10 version 2004
            {
                var dialog = new WindowsVersionIncompatibleDialog();

                await ContentDialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
                {
                    Title = LocalizationUtils.GetString("WindowsVersionIncompatible"),
                    Content = dialog,
                    CloseButtonText = LocalizationUtils.GetString("Ok"),
                });

                if(dialog.Acknowledged == true)
                {
                    ConfigProvider.Config!.VersionWarningAcknowledged = true;
                    ConfigProvider.Save();
                }
            }

            while (App.DeferredContentDialogs.Count > 0)
            {
                var contentDialog = App.DeferredContentDialogs.Dequeue();
                await ContentDialogService.ShowSimpleDialogAsync(contentDialog);
            }
        }

        [RelayCommand]
        private void ShowWindow()
        {
            App.Current.MainWindow.Activate();
        }

        [RelayCommand]
        private void QuitProgram()
        {
            App.Current.Shutdown();
        }

        [ObservableProperty]
        private string _applicationTitle = "HideMyWindows";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = LocalizationUtils.GetString("Home"),
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = LocalizationUtils.GetString("QuickLaunch"),
                Icon = new SymbolIcon { Symbol = SymbolRegular.Rocket24 },
                TargetPageType = typeof(Views.Pages.QuickLaunchPage)
            },
            new NavigationViewItem()
            {
                Content = LocalizationUtils.GetString("WindowRules"),
                Icon = new SymbolIcon { Symbol = SymbolRegular.List24 },
                TargetPageType = typeof(Views.Pages.WindowRulesPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = LocalizationUtils.GetString("Settings"),
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };
    }
}
