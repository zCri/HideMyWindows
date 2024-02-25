using HideMyWindows.App.Services.DllInjector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.User32;


namespace HideMyWindows.App.Services.WindowHider
{
    public class DllInjectorWindowHider : IWindowHider
    {
        private IDllInjector DllInjector { get; }

        public DllInjectorWindowHider(IDllInjector dllInjector)
        {
            DllInjector = dllInjector;
        }

        public void ApplyAction(WindowHiderAction action, Process process)
        {
            var handle = DllInjector.InjectDll(process);

            switch (action)
            {
                case WindowHiderAction.HideProcess:
                    DllInjector.HideAllWindows(process, handle);
                    break;
                case WindowHiderAction.HideWindow:
                    DllInjector.HideWindow(process, handle, process.MainWindowHandle);
                    break;
            }
        }

        public void ApplyAction(WindowHiderAction action, IntPtr hwnd)
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            var process = Process.GetProcessById((int)pid);

            var handle = DllInjector.InjectDll(process);

            switch (action)
            {
                case WindowHiderAction.HideProcess:
                    DllInjector.HideAllWindows(process, handle);
                    break;
                case WindowHiderAction.HideWindow:
                    DllInjector.HideWindow(process, handle, hwnd);
                    break;
            }
        }
    }
}
