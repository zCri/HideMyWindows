using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Kernel32;

namespace HideMyWindows.App.Services.WindowClickFinder
{
    public class MouseCaptureWindowClickFinder : IWindowClickFinder
    {
        private readonly HwndSource source = (HwndSource) PresentationSource.FromVisual(Application.Current.MainWindow);

        // Not thread safe
        private TaskCompletionSource<WindowInfo>? tcs;

        IntPtr WndProc(IntPtr _, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int) WindowMessage.WM_LBUTTONDOWN)
            {
                GetCursorPos(out var pos);
                IntPtr hwnd = (IntPtr) WindowFromPoint(pos);

                var gwlStyle = GetWindowLongPtr(hwnd, WindowLongFlags.GWL_STYLE); // If hwnd is a handle to a control, get the parent window
                if((gwlStyle.ToInt64() & (long)WindowStyles.WS_CHILD) > 0)
                {
                    hwnd = (IntPtr) GetAncestor(hwnd, GetAncestorFlag.GA_ROOT);
                }

                var titleLen = GetWindowTextLength(hwnd) + 1;
                var titleBuilder = new StringBuilder(titleLen);
                if(GetWindowText(hwnd, titleBuilder, titleLen) == 0) GetLastError().ThrowIfFailed();

                var classBuilder = new StringBuilder(1024);
                if (GetClassName(hwnd, classBuilder, 1024) == 0) GetLastError().ThrowIfFailed();

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
