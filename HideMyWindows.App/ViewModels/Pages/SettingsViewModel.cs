// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Helpers;
using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Services.ProcessWatcher;
using HideMyWindows.App.Services.WindowWatcher;
using System.Reflection;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace HideMyWindows.App.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject
    {
        public IConfigProvider ConfigProvider { get; }
        private ISnackbarService SnackbarService { get; }

        public SettingsViewModel(IConfigProvider configProvider, ISnackbarService snackbarService)
        {
            ConfigProvider = configProvider;
            SnackbarService = snackbarService;

            configProvider.Load();
            ConfigProvider.Config!.CurrentTheme = ApplicationThemeManager.GetAppTheme();
            AppVersion = $"HideMyWindows.App - {GetAssemblyVersion()}";
        }

        public ProcessWatcherType? ProcessWatcherType { 
            get => ConfigProvider.Config!.ProcessWatcherType;
            set
            {
                ConfigProvider.Config!.ProcessWatcherType = value ?? default;
                SnackbarService.Show("Settings", "To apply these settings, save the settings and restart the application", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24));
            }
        }
        public WindowWatcherType? WindowWatcherType { 
            get => ConfigProvider.Config!.WindowWatcherType; 
            set 
            {
                ConfigProvider.Config!.WindowWatcherType = value ?? default;
                SnackbarService.Show("Settings", "To apply these settings, save the settings and restart the application", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24));
            }
        }

        [ObservableProperty]
        private string _appVersion = string.Empty;

        private string GetAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? string.Empty;
        }

        [RelayCommand]
        private void ChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    ConfigProvider.Config!.CurrentTheme = ApplicationTheme.Light;
                    break;

                default:
                    ConfigProvider.Config!.CurrentTheme = ApplicationTheme.Dark;
                    break;
            }
        }

        [RelayCommand]
        private void SaveConfig()
        {
            ConfigProvider.Save();
        }
    }
}
