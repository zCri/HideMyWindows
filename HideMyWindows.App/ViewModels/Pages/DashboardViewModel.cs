// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Helpers;
using HideMyWindows.App.Services.DllInjector;
using HideMyWindows.App.Services.ProcessWatcher;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using static Vanara.PInvoke.Kernel32;

namespace HideMyWindows.App.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private IDllInjector DllInjector { get; }
        private IProcessWatcher ProcessWatcher { get; }
        private ISnackbarService SnackbarService { get; }

        public DashboardViewModel(IDllInjector dllInjector, IProcessWatcher processWatcher, ISnackbarService snackbarService)
        {
            DllInjector = dllInjector;
            ProcessWatcher = processWatcher;
            SnackbarService = snackbarService;

            // TODO: Fix combobox selected item reset on RunningProcesses changed
            ProcessWatcher.ProcessStopped += (_, _) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InjectIntoProcessCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(RunningProcesses));
                });
            };

            ProcessWatcher.ProcessStarted += (_, _) =>
                Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(nameof(RunningProcesses)));
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartProcessCommand))]
        private string _processPath = string.Empty;

        [ObservableProperty]
        private string _processArguments = string.Empty;

        public IEnumerable<ProcessProxy> RunningProcesses { get => Process.GetProcesses().Select(process => new ProcessProxy(process)); }

        //TODO: Select process by window click button
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(InjectIntoProcessCommand))]
        private ProcessProxy? _selectedProcess;

        [RelayCommand(CanExecute = nameof(CanStartProcess))]
        private void StartProcess()
        {
            try
            {
                var commandLine = new StringBuilder(ProcessArguments);
                var workingDirectory = new FileInfo(ProcessPath)?.Directory?.FullName;

                if (!CreateProcess(ProcessPath, commandLine, null, null, true, CREATE_PROCESS.CREATE_SUSPENDED, null, workingDirectory, STARTUPINFO.Default, out var processInformation))
                    throw GetLastError().GetException();
                var process = Process.GetProcessById((int) processInformation.dwProcessId);

                DllInjector.InjectDll(process);

                if((int) ResumeThread(processInformation.hThread) == -1)
                    throw GetLastError().GetException();

            } catch (Exception e) 
            {
                //TODO: handle errors
                SnackbarService.Show("An error occurred!", e.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24));
            }
        }

        private bool CanStartProcess()
        {
            return !(string.IsNullOrEmpty(ProcessPath) || !File.Exists(ProcessPath));
        }

        [RelayCommand]
        private void BrowsePath()
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Executable file|*.exe|All files|*.*",
                FileName = ProcessPath,
            };
            
            bool? result = dialog.ShowDialog();
            
            if(result == true)
            {
                ProcessPath = dialog.FileName;
            }
        }

        [RelayCommand]
        private void InjectIntoProcess()
        {
            try
            {
                if(SelectedProcess is not null)
                    DllInjector.InjectDll(SelectedProcess.Process);
            } catch (Exception e)
            {
                SnackbarService.Show("An error occurred!", e.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24));
            }
        }

        private bool CanInjectIntoProcess()
        {
            return !SelectedProcess?.Process.HasExited ?? false;
        }
    }
}
