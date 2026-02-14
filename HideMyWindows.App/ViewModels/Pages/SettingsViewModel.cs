// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Helpers;
using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Services.ProcessWatcher;
using HideMyWindows.App.Services.TourService;
using HideMyWindows.App.Services.WindowWatcher;
using System.Globalization;
using System.IO;
using System.Reflection;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using WPFLocalizeExtension.Engine;
using static Vanara.PInvoke.Shell32;

namespace HideMyWindows.App.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject
    {
        public IConfigProvider ConfigProvider { get; }
        private ISnackbarService SnackbarService { get; }
        private ITourService TourService { get; }

        public SettingsViewModel(IConfigProvider configProvider, ISnackbarService snackbarService, ITourService tourService)
        {
            ConfigProvider = configProvider;
            SnackbarService = snackbarService;
            TourService = tourService;

            configProvider.Load();
            ConfigProvider.Config!.CurrentTheme = ApplicationThemeManager.GetAppTheme();
            AppVersion = $"HideMyWindows — {GetAssemblyVersion()}";
            AvailableCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures).Where(culture => Directory.Exists(culture.TwoLetterISOLanguageName) || culture.TwoLetterISOLanguageName == "en");
        }

        public ProcessWatcherType? ProcessWatcherType { 
            get => ConfigProvider.Config!.ProcessWatcherType;
            set
            {
                ConfigProvider.Config!.ProcessWatcherType = value ?? default;
                SnackbarService.Show(LocalizationUtils.GetString("Settings"), LocalizationUtils.GetString("ToApplyTheseSettingsSaveAndRestart"), ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24));
            }
        }

        public WindowWatcherType? WindowWatcherType { 
            get => ConfigProvider.Config!.WindowWatcherType; 
            set 
            {
                ConfigProvider.Config!.WindowWatcherType = value ?? default;
                SnackbarService.Show(LocalizationUtils.GetString("Settings"), LocalizationUtils.GetString("ToApplyTheseSettingsSaveAndRestart"), ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24));
            }
        }

        public int? ProcessWatcherDelay {
            get => ConfigProvider.Config!.WMIInstanceEventProcessWatcherTimeoutMillis;
            set
            {
                ConfigProvider.Config!.WMIInstanceEventProcessWatcherTimeoutMillis = value ?? default;
                SnackbarService.Show(LocalizationUtils.GetString("Settings"), LocalizationUtils.GetString("ToApplyTheseSettingsSaveAndRestart"), ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24));
            }
        }

        public int? RuleReapplyIntervalMs {
            get => ConfigProvider.Config!.RuleReapplyIntervalMs;
            set
            {
                ConfigProvider.Config!.RuleReapplyIntervalMs = value ?? default;
                SnackbarService.Show(LocalizationUtils.GetString("Settings"), LocalizationUtils.GetString("ToApplyTheseSettingsSaveAndRestart"), ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24));
            }
        }

        public IEnumerable<CultureInfo> AvailableCultures { get; init; }

        public CultureInfo SelectedCulture { 
            get => ConfigProvider.Config!.SelectedCulture ?? 
                (CultureInfo.CurrentUICulture.IsNeutralCulture
                    ? CultureInfo.CurrentUICulture
                    : CultureInfo.CurrentUICulture.Parent);
            set {
                ConfigProvider.Config!.SelectedCulture = value;
                CultureInfo.CurrentUICulture = value;
                LocalizeDictionary.Instance.Culture = value;
                SnackbarService.Show(LocalizationUtils.GetString("Settings"), LocalizationUtils.GetString("ToApplyTheseSettingsSaveAndRestart"), ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24));
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
        private void RestartTour()
        {
            TourService.Start();
        }

        [RelayCommand]
        private void ResetCultureChoice()
        {
            ConfigProvider.Config!.SelectedCulture = null;
            CultureInfo.CurrentUICulture = App.OriginalCulture;
            LocalizeDictionary.Instance.Culture = App.OriginalCulture;
            SnackbarService.Show(LocalizationUtils.GetString("Settings"), LocalizationUtils.GetString("ToApplyTheseSettingsSaveAndRestart"), ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24));
            OnPropertyChanged(nameof(SelectedCulture));
        }

        [RelayCommand]
        private void SaveConfig()
        {
            ConfigProvider.Save();
        }

        [RelayCommand]
        private void OpenConfigFolder()
        {
            try
            {
                var configPath = ConfigProvider.ConfigPath;
                var folderPath = Path.GetFullPath(Path.GetDirectoryName(configPath)!);
                var filePath = Path.GetFullPath(configPath);

                SHParseDisplayName(folderPath, IntPtr.Zero, out var folderPidl, 0, out _).ThrowIfFailed();
                if (!folderPidl) return;

                SHParseDisplayName(filePath, IntPtr.Zero, out var filePidl, 0, out _).ThrowIfFailed();
                if (!filePidl) return;

                using (folderPidl)
                using (filePidl)
                {
                    IntPtr[] filePidls = [filePidl.DangerousGetHandle()];
                    SHOpenFolderAndSelectItems(folderPidl, (uint)filePidls.Length, filePidls, OFASI.OFASI_NONE);
                }
            } catch (Exception e)
            {
                SnackbarService.Show(LocalizationUtils.GetString("AnErrorOccurred"), e.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24));
            }
        }
    }
}
