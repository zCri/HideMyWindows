// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Controls;
using HideMyWindows.App.Services;
using HideMyWindows.App.Services.ConfigProvider;
using System.Collections.ObjectModel;
using Wpf.Ui.Controls;
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

                await ContentDialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions // TODO: Localization
                {
                    Title = "Incompatible Windows version",
                    Content = dialog,
                    CloseButtonText = "OK",
                });

                if(dialog.Acknowledged == true)
                {
                    ConfigProvider.Config!.VersionWarningAcknowledged = true;
                    ConfigProvider.Save();
                }
                
            }
        }

        [ObservableProperty]
        private string _applicationTitle = "HideMyWindows";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Home",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = "Quick launch",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Rocket24 },
                TargetPageType = typeof(Views.Pages.QuickLaunchPage)
            },
            new NavigationViewItem()
            {
                Content = "Window rules",
                Icon = new SymbolIcon { Symbol = SymbolRegular.List24 },
                TargetPageType = typeof(Views.Pages.WindowRulesPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };
    }
}
