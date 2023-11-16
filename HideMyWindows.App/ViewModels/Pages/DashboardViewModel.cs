// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using HideMyWindows.App.Services.DllInjector;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using static Vanara.PInvoke.Kernel32;

namespace HideMyWindows.App.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        public IDllInjector DllInjector { get; }

        public DashboardViewModel(IDllInjector dllInjector)
        {
            DllInjector = dllInjector;
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartProcessCommand))]
        private string _processPath = string.Empty;

        [ObservableProperty]
        private string _processArguments = string.Empty;

        public IEnumerable<Process> RunningProcesses { get => Process.GetProcesses(); }

        [RelayCommand(CanExecute = nameof(CanStartProcess))]
        private void StartProcess()
        {
            try
            {
                //TODO: if process forces runas
                var commandLine = new StringBuilder(ProcessArguments);
                var workingDirectory = new FileInfo(ProcessPath)?.Directory?.FullName;

                if (!CreateProcess(ProcessPath, commandLine, null, null, true, CREATE_PROCESS.CREATE_SUSPENDED, null, workingDirectory, STARTUPINFO.Default, out var processInformation))
                    throw new Win32Exception(GetLastError().ToHRESULT().Code);
                var process = Process.GetProcessById((int) processInformation.dwProcessId);

                DllInjector.InjectDll(process);

                if((int) ResumeThread(processInformation.hThread) == -1)
                    throw new Win32Exception(GetLastError().ToHRESULT().Code);

            } catch (Exception e) when (e is InvalidOperationException or Win32Exception or ObjectDisposedException or FileNotFoundException) 
            {
                //TODO: handle errors
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
    }
}
