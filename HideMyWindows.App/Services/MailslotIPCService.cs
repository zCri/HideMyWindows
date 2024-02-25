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
using static Vanara.PInvoke.Kernel32;

namespace HideMyWindows.App.Services
{
    public class MailslotIPCService : IHostedService
    {
        private IDllInjector DllInjector { get; }
        private CancellationTokenSource CancellationTokenSource { get; }
        private SafeMailslotHandle? Mailslot { get; set; }
        
        public MailslotIPCService(IDllInjector dllInjector)
        {
            DllInjector = dllInjector;
            CancellationTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                Mailslot = CreateMailslot(@"\\.\mailslot\HideMyWindowsMailslot", 0, MAILSLOT_WAIT_FOREVER, null);
                if (Mailslot.IsInvalid)
                {
                    throw GetLastError().GetException();
                }

                var @event = new ManualResetEvent(false);
                var overlapped = new NativeOverlapped()
                {
                    EventHandle = @event.SafeWaitHandle.DangerousGetHandle()
                };

                var buffer = new byte[128];
                var pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                try
                {
                    while (!CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        if (!ReadFile(Mailslot.DangerousGetHandle(), buffer, sizeof(byte) * 128, out _, ref overlapped))
                        {
                            GetLastError().ThrowUnless(Win32Error.ERROR_IO_PENDING);
                        }
                        WaitHandle.WaitAny(new WaitHandle[] { CancellationTokenSource.Token.WaitHandle, @event });

                        if(!CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            OnMessageReceived(Encoding.Unicode.GetString(buffer));
                        }
                    }
                } finally
                {
                    pin.Free();
                }
            }, cancellationToken);
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
            var data = msg.Split("|");

            if (!int.TryParse(data[0], out int msgId)) return;
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
            }
        }
    }
}
