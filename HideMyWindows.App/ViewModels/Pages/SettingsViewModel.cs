// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Services.ProcessWatcher;
using HideMyWindows.App.Services.WindowWatcher;
using System.Reflection;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

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
            ConfigProvider.Config!.CurrentTheme = Theme.GetAppTheme();
            AppVersion = $"HideMyWindows.App - {GetAssemblyVersion()}";
        }

        public ProcessWatcherType? ProcessWatcherType { 
            get => ConfigProvider.Config?.ProcessWatcherType;
            set
            {
                if(ConfigProvider.Config is not null) ConfigProvider.Config.ProcessWatcherType = value ?? default;
                SnackbarService.Show("Settings", "To apply these settings, save the settings and restart the application", ControlAppearance.Info);
            }
        }
        public WindowWatcherType? WindowWatcherType { 
            get => ConfigProvider.Config?.WindowWatcherType; 
            set 
            {
                if (ConfigProvider.Config is not null)  ConfigProvider.Config.WindowWatcherType = value ?? default;
                SnackbarService.Show("Settings", "To apply these settings, save the settings and restart the application", ControlAppearance.Info);
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
                    ConfigProvider.Config!.CurrentTheme = ThemeType.Light;
                    break;

                default:
                    ConfigProvider.Config!.CurrentTheme = ThemeType.Dark;
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
