using HideMyWindows.App.Helpers;
using HideMyWindows.App.Services.DllInjector;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Vanara.PInvoke;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using static Vanara.PInvoke.Kernel32;

namespace HideMyWindows.App.Services
{
    public class MailslotIPCService : IHostedService
    {
        private IDllInjector DllInjector { get; }
        private CancellationTokenSource CancellationTokenSource { get; }
        private ISnackbarService SnackbarService { get; }
        private SafeMailslotHandle? Mailslot { get; set; }
        
        public MailslotIPCService(IDllInjector dllInjector, ISnackbarService snackbarService)
        {
            DllInjector = dllInjector;
            CancellationTokenSource = new CancellationTokenSource();
            SnackbarService = snackbarService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Factory.StartNew(() =>
            {
                Mailslot = CreateMailslot(@"\\.\mailslot\HideMyWindowsMailslot", 0, MAILSLOT_WAIT_FOREVER, null);
                if (Mailslot.IsInvalid) return;

                using var @event = new ManualResetEvent(false);
                var buffer = new byte[1024];
                var pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                try
                {
                    while (!CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        @event.Reset();

                        var overlapped = new NativeOverlapped()
                        {
                            EventHandle = @event.SafeWaitHandle.DangerousGetHandle()
                        };

                        bool immediateSuccess = ReadFile(Mailslot.DangerousGetHandle(), buffer, buffer.Length, out _, ref overlapped);

                        if (!immediateSuccess) GetLastError().ThrowUnless(Win32Error.ERROR_IO_PENDING);

                        int waitResult = WaitHandle.WaitAny([CancellationTokenSource.Token.WaitHandle, @event]);
                        if (waitResult == 1 && !CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            string msg = Encoding.Unicode.GetString(buffer).TrimEnd('\0');

                            if (!string.IsNullOrEmpty(msg))
                            {
                                OnMessageReceived(msg);
                            }
                        }
                    }
                }
                finally
                {
                    if (pin.IsAllocated) pin.Free();
                    Mailslot.Dispose();
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            CancellationTokenSource.Cancel();
            Mailslot?.Dispose();
            return Task.CompletedTask;
        }

        private void OnMessageReceived(string msg)
        {
            var data = msg.Split('|', StringSplitOptions.RemoveEmptyEntries);

            if (data.Length == 0 || !int.TryParse(data[0], out int msgId))
                return;

            switch(msgId)
            {
                case 0:
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            var window = Application.Current?.MainWindow;
                            if (window is not null)
                            {
                                window.Activate();
                                window.Topmost = true;
                                window.Topmost = false;
                                window.Focus();
                            }
                        });
                        break;
                    }
                case 1:
                    {
                        if (data.Length < 2 || !int.TryParse(data[1], out int pid)) return;

                        try
                        {
                            var process = Process.GetProcessById(pid);

                            var handle = DllInjector.InjectDll(process);
                            DllInjector.HideAllWindows(process, handle);
                        }
                        catch (ArgumentException) { }
                        break;
                    }
                case 2:
                    {
                        if (data.Length < 3 || !int.TryParse(data[1], out int logType)) return;

                        var logMsg = string.Join("|", data[2..]);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            switch (logType)
                            {
                                case 0:
                                    SnackbarService.Show(
                                        LocalizationUtils.GetString("Info"),
                                        logMsg,
                                        ControlAppearance.Info,
                                        new SymbolIcon(SymbolRegular.Info24)
                                    );
                                    break;

                                case 1:
                                    SnackbarService.Show(
                                        LocalizationUtils.GetString("Warning"),
                                        logMsg,
                                        ControlAppearance.Caution,
                                        new SymbolIcon(SymbolRegular.ErrorCircle24)
                                    );
                                    break;

                                case 2:
                                    SnackbarService.Show(
                                        LocalizationUtils.GetString("AnErrorOccurred"),
                                        logMsg,
                                        ControlAppearance.Danger,
                                        new SymbolIcon(SymbolRegular.ErrorCircle24)
                                    );
                                    break;
                            }
                        });
                        break;
                    }
            }
        }
    }
}
