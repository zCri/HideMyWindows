using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace HideMyWindows.App.Services.WindowClickFinder
{
    public class MouseCaptureWindowClickFinder : IWindowClickFinder
    {
        private readonly HwndSource source = (HwndSource) PresentationSource.FromVisual(Application.Current.MainWindow);

        // Not thread safe
        private TaskCompletionSource<WindowInfo>? tcs;

        IntPtr WndProc(IntPtr _, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle messages...
            if (msg == (int) WindowMessage.WM_LBUTTONDOWN)
            {
                GetCursorPos(out var pos);
                IntPtr hwnd = (IntPtr) WindowFromPoint(pos);

                var titleLen = GetWindowTextLength(hwnd) + 1;
                var titleBuilder = new StringBuilder(titleLen);
                GetWindowText(hwnd, titleBuilder, titleLen);

                var classBuilder = new StringBuilder(1024);
                GetClassName(hwnd, classBuilder, 1024);

                GetWindowThreadProcessId(hwnd, out var pid);

                var windowInfo = new WindowInfo
                {
                    Handle = hwnd,
                    Title = titleBuilder.ToString(),
                    Class = classBuilder.ToString(),
                    Process = Process.GetProcessById((int) pid)
                };

                ReleaseCapture();
                source.RemoveHook(WndProc);
                tcs!.SetResult(windowInfo);
                handled = true;
            }

            return IntPtr.Zero;
        }

        public Task<WindowInfo> FindWindowByClickAsync()
        {
            SetCapture(source.Handle);
            source.AddHook(WndProc);
            tcs = new TaskCompletionSource<WindowInfo>();
            return tcs.Task;
        }
    }
}
