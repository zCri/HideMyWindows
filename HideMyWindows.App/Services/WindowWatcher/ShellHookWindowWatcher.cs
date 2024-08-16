using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Kernel32;


namespace HideMyWindows.App.Services.WindowWatcher
{
    internal class ShellHookWindowWatcher : IWindowWatcher
    {
        public event EventHandler<WindowWatchedEventArgs>? WindowCreated;
        public event EventHandler<WindowWatchedEventArgs>? WindowDestroyed;

        public bool IsWatching { get; private set; } = false;

        private uint ShellHookMsg { get; set; } = 0;

        protected virtual void OnWindowCreated(WindowWatchedEventArgs e)
        {
            WindowCreated?.Invoke(this, e);
        }

        protected virtual void OnWindowDestroyed(WindowWatchedEventArgs e)
        {
            WindowDestroyed?.Invoke(this, e);
        }

        public void Initialize(HWND hwnd)
        {
            var source = HwndSource.FromHwnd(hwnd.DangerousGetHandle());

            source.AddHook(WndProc);

            ShellHookMsg = RegisterWindowMessage("SHELLHOOK");
            if (ShellHookMsg == 0) throw GetLastError().GetException();
            if (!RegisterShellHookWindow(hwnd)) throw GetLastError().GetException();
            IsWatching = true;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if((uint) msg == ShellHookMsg)
            {
                if(wParam.ToInt64() == 1) // HSHELL_WINDOWCREATED
                {
                    OnWindowCreated(GetEventArgs(lParam));
                    handled = true;
                }
                if(wParam.ToInt64() == 2) // HSHELL_WINDOWDESTROYED
                {
                    OnWindowDestroyed(GetEventArgs(lParam));
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private WindowWatchedEventArgs GetEventArgs(HWND hwnd)
        {
            var titleLen = GetWindowTextLength(hwnd) + 1;
            var titleBuilder = new StringBuilder(titleLen);
#pragma warning disable CS0642 // Possible mistaken empty statement
            if (GetWindowText(hwnd, titleBuilder, titleLen) == 0) ; // Ignore

            var classBuilder = new StringBuilder(1024);
            if (GetClassName(hwnd, classBuilder, 1024) == 0) ; // Ignore
#pragma warning restore CS0642 // Possible mistaken empty statement

            return new WindowWatchedEventArgs
            {
                Handle = hwnd.DangerousGetHandle(),
                Title = titleBuilder.ToString(),
                Class = classBuilder.ToString()
            };
        }
    }
}
