// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Helpers;
using HideMyWindows.App.Models;
using HideMyWindows.App.Services.DllInjector;
using HideMyWindows.App.Services.ProcessWatcher;
using HideMyWindows.App.Services.WindowClickFinder;
using Microsoft.Win32;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.User32;

namespace HideMyWindows.App.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private IDllInjector DllInjector { get; }
        private IProcessWatcher ProcessWatcher { get; }
        private ISnackbarService SnackbarService { get; }
        private IWindowClickFinder WindowClickFinder { get; }

        public DashboardViewModel(IDllInjector dllInjector, IProcessWatcher processWatcher, ISnackbarService snackbarService, IWindowClickFinder windowClickFinder)
        {
            DllInjector = dllInjector;
            ProcessWatcher = processWatcher;
            SnackbarService = snackbarService;
            WindowClickFinder = windowClickFinder;

            ProcessWatcher.ProcessStarted += (_, e) =>
            {
                Application.Current?.Dispatcher.Invoke(() => {
                    RunningProcesses.Add(new ProcessProxy(Process.GetProcessById(e.Id)));
                });
            };

            ProcessWatcher.ProcessStopped += (_, e) =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    RunningProcesses.Remove(RunningProcesses.First(x => x.Process.Id == e.Id));
                    InjectIntoProcessCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(RunningProcesses));
                });
            };
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartProcessCommand))]
        private string _processPath = string.Empty;

        [ObservableProperty]
        private string _processArguments = string.Empty;

        [ObservableProperty]
        private BindingList<ProcessProxy> _runningProcesses = new(Process.GetProcesses().Select(process => new ProcessProxy(process)).ToList());

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(InjectIntoProcessCommand))]
        private ProcessProxy? _selectedProcess;

        [ObservableProperty]
        private WindowRule _findWindowRule = new();

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
                //TODO: Localization
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

        [RelayCommand]
        private async Task FindProcessByClick()
        {
            var window = await WindowClickFinder.FindWindowByClickAsync();

            if(window.Process is not null)
                SelectedProcess = RunningProcesses.First(process => process.Process.Id == window.Process.Id);
        }

        [RelayCommand]
        private async Task FindAndHide()
        {
            if(FindWindowRule.Target is WindowRuleTarget.WindowTitle or WindowRuleTarget.WindowClass)
            {
                await Task.Run(() => {
                    EnumWindows((hwnd, _) =>
                    {
                        string value = string.Empty;

                        switch(FindWindowRule.Target)
                        {
                            case WindowRuleTarget.WindowTitle:
                            {
                                var titleLen = GetWindowTextLength(hwnd) + 1;
                                var titleBuilder = new StringBuilder(titleLen);
                                GetWindowText(hwnd, titleBuilder, titleLen);
                                value = titleBuilder.ToString();
                                break;
                            }
                            case WindowRuleTarget.WindowClass:
                            {
                                var classBuilder = new StringBuilder(1024);
                                GetClassName(hwnd, classBuilder, 1024);
                                value = classBuilder.ToString();
                                break;
                            }
                        }

                        if (FindWindowRule.Matches(value))
                        {
                            GetWindowThreadProcessId(hwnd, out var pid);
                            var process = Process.GetProcessById((int) pid);
                            DllInjector.InjectDll(process);
                        }

                        return true;
                    }, IntPtr.Zero);
                });
            } else
            {
                if (FindWindowRule.Comparator is WindowRuleComparator.StringEquals && FindWindowRule.Target is WindowRuleTarget.ProcessId)
                {
                    try
                    {
                        var process = Process.GetProcessById(int.Parse(FindWindowRule.Value));

                        DllInjector.InjectDll(process);
                    } //TODO: Localization
                    catch (ArgumentException e)
                    {
                        SnackbarService.Show("No process found!", e.Message, ControlAppearance.Info, new SymbolIcon(SymbolRegular.Search24));
                    }
                    catch (Exception e)
                    {
                        SnackbarService.Show("An error occurred!", e.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24));
                    }

                }
                else
                {
                    await Task.Run(() =>
                    {
                        var processes = Process.GetProcesses();

                        foreach (var process in processes)
                        {
                            var value = FindWindowRule.Target switch
                            {
                                WindowRuleTarget.ProcessName => process.TryGetProcessNameWithExtension(),
                                WindowRuleTarget.ProcessId => process.Id.ToString(),
                                _ => throw new NotImplementedException(),
                            };

                            if (FindWindowRule.Matches(value))
                            {
                                DllInjector.InjectDll(process);
                            }
                        }
                    });
                }
            }
        }

        [RelayCommand]
        private async Task FindWindowRuleTargetByClick()
        {
            var windowInfo = await WindowClickFinder.FindWindowByClickAsync();
            FindWindowRule.Value = FindWindowRule.Target switch
            {
                WindowRuleTarget.ProcessName => windowInfo.Process?.TryGetProcessNameWithExtension()!,
                WindowRuleTarget.ProcessId => (windowInfo.Process?.Id ?? 0).ToString(),
                WindowRuleTarget.WindowClass => windowInfo.Class,
                WindowRuleTarget.WindowTitle => windowInfo.Title,
                _ => ""
            };
        }
    }
}
