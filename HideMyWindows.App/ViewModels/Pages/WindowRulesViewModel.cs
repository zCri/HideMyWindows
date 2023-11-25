// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Models;
using System.Windows.Media;
using Wpf.Ui.Controls;
using HideMyWindows.App.Services.ConfigProvider;

namespace HideMyWindows.App.ViewModels.Pages
{
    public partial class WindowRulesViewModel : ObservableObject
    {
        public IConfigProvider ConfigProvider { get; }

        public WindowRulesViewModel(IConfigProvider configProvider)
        {
            ConfigProvider = configProvider;
        }

        [RelayCommand]
        private void SaveConfig()
        {
            ConfigProvider.Save();
        }

        [RelayCommand]
        private void AddRule()
        {
            ConfigProvider.Config?.WindowRules.AddNew();
        }

        [RelayCommand]
        private void RemoveRule(WindowRule rule)
        {
            ConfigProvider?.Config?.WindowRules.Remove(rule);
        }
    }
}
