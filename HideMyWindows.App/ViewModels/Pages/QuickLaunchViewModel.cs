using HideMyWindows.App.Models;
using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Services.DllInjector;
using static Vanara.PInvoke.Kernel32;
using System.IO;
using System.Text;
using Wpf.Ui.Controls;
using System.Diagnostics;
using HideMyWindows.App.Helpers;
using Vanara.PInvoke;
using HideMyWindows.App.Controls;
using HideMyWindows.App.Services.WindowHider;
using WPFLocalizeExtension.Engine;
using Wpf.Ui.Extensions;

namespace HideMyWindows.App.ViewModels.Pages
{
    public partial class QuickLaunchViewModel : ObservableObject
    {
        public IConfigProvider ConfigProvider { get; }
        private IWindowHider WindowHider { get; }
        private ISnackbarService SnackbarService { get; }
        private IContentDialogService ContentDialogService { get; }

        public QuickLaunchViewModel(IConfigProvider configProvider, IWindowHider windowHider, ISnackbarService snackbarService, IContentDialogService contentDialogService)
        {
            ConfigProvider = configProvider;
            WindowHider = windowHider;
            SnackbarService = snackbarService;
            ContentDialogService = contentDialogService;

            ConfigProvider.Load();
        }

        [RelayCommand]
        private async Task AddQuickLaunchEntryAsync()
        {
            var entry = new QuickLaunchEntry();
            var result = await EditQuickLaunchEntryAsync(entry);

            if(result == ContentDialogResult.Primary)
                ConfigProvider.Config!.QuickLaunchEntries.Add(entry);
        }

        [RelayCommand]
        private void RemoveQuickLaunchEntry(QuickLaunchEntry entry)
        {
            ConfigProvider.Config!.QuickLaunchEntries.Remove(entry);
        }

        [RelayCommand]
        private async Task<ContentDialogResult> EditQuickLaunchEntryAsync(QuickLaunchEntry entry)
        {
            var editControl = new QuickLaunchEntryEditControl()
            {
                Path = entry.Path,
                Arguments = entry.Arguments,
            };

            var result = await ContentDialogService.ShowSimpleDialogAsync(new() {
                Title = LocalizeDictionary.Instance.GetLocalizedObject("HideMyWindows.App", "Strings", "EditEntry", LocalizeDictionary.CurrentCulture) as string ?? string.Empty,
                Content = editControl,
                PrimaryButtonText = LocalizeDictionary.Instance.GetLocalizedObject("HideMyWindows.App", "Strings", "Edit", LocalizeDictionary.CurrentCulture) as string ?? string.Empty,
                CloseButtonText = LocalizeDictionary.Instance.GetLocalizedObject("HideMyWindows.App", "Strings", "Cancel", LocalizeDictionary.CurrentCulture) as string ?? string.Empty,
            });

            if (result == ContentDialogResult.Primary)
            {
                entry.Name = editControl.Name ?? string.Empty;
                if(string.IsNullOrEmpty(entry.Name))
                    entry.Name = Path.GetFileName(editControl.Path);

                entry.Path = editControl.Path;
                entry.Arguments = editControl.Arguments;
            }

            return result;
        }

        [RelayCommand]
        private void RunQuickLaunchEntry(QuickLaunchEntry entry)
        {
            try
            {
                var commandLine = new StringBuilder(entry.Arguments);
                var workingDirectory = new FileInfo(entry.Path)?.Directory?.FullName;

                if (!CreateProcess(entry.Path, commandLine, null, null, true, CREATE_PROCESS.CREATE_SUSPENDED, null, workingDirectory, STARTUPINFO.Default, out var processInformation))
                    throw GetLastError().GetException();
                var process = Process.GetProcessById((int)processInformation.dwProcessId);

                WindowHider.ApplyAction(WindowHiderAction.HideProcess, process);

                if ((int)ResumeThread(processInformation.hThread) == -1)
                    throw GetLastError().GetException();

            }
            catch (Exception e)
            {
                SnackbarService.Show(LocalizeDictionary.Instance.GetLocalizedObject("HideMyWindows.App", "Strings", "AnErrorOccurred", LocalizeDictionary.CurrentCulture) as string ?? string.Empty, e.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24));
            }
        }

        [RelayCommand]
        private void SaveConfig()
        {
            ConfigProvider.Save();
        }
    }
}
