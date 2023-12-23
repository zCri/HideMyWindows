// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Models;
using System.Windows.Media;
using Wpf.Ui.Controls;
using HideMyWindows.App.Services.ConfigProvider;
using static Vanara.PInvoke.User32;
using HideMyWindows.App.Services.WindowClickFinder;
using System.IO;
using HideMyWindows.App.Helpers;

namespace HideMyWindows.App.ViewModels.Pages
{
    public partial class WindowRulesViewModel : ObservableObject
    {
        public IConfigProvider ConfigProvider { get; }
        private IWindowClickFinder WindowClickFinder { get; }

        public WindowRulesViewModel(IConfigProvider configProvider, IWindowClickFinder windowClickFinder)
        {
            ConfigProvider = configProvider;
            WindowClickFinder = windowClickFinder;
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
        private async Task FindByClick(WindowRule rule)
        {
            var windowInfo = await WindowClickFinder.FindWindowByClickAsync();
            rule.Value = rule.Target switch
            {
                WindowRuleTarget.ProcessName => windowInfo.Process?.TryGetProcessNameWithExtension()!,
                WindowRuleTarget.ProcessId => (windowInfo.Process?.Id ?? 0).ToString(),
                WindowRuleTarget.WindowClass => windowInfo.Class,
                WindowRuleTarget.WindowTitle => windowInfo.Title,
                _ => ""
            };
        }

        [RelayCommand]
        private void RemoveRule(WindowRule rule)
        {
            ConfigProvider?.Config?.WindowRules.Remove(rule);
        }
    }
}
