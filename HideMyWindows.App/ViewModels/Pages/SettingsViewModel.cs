// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Services.ConfigProvider;
using System.Reflection;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace HideMyWindows.App.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject
    {
        public IConfigProvider ConfigProvider { get; }

        [ObservableProperty]
        private string _appVersion = string.Empty;

        public SettingsViewModel(IConfigProvider configProvider)
        {
            ConfigProvider = configProvider;

            configProvider.Load();
            ConfigProvider.Config!.CurrentTheme = Theme.GetAppTheme();
            AppVersion = $"HideMyWindows.App - {GetAssemblyVersion()}";
        }

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
