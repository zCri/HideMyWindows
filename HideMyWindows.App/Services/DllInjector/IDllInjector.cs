using HideMyWindows.App.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Services.DllInjector
{
    public interface IDllInjector
    {
        public IntPtr InjectDll(Process process);

        public void InvokeDllMethod<T>(Process process, IntPtr handle, string methodName, T parameter) where T : struct
        {
            InvokeDllMethod(process, handle, methodName, parameter.ToBytes());
        }

        public void InvokeDllMethod(Process process, IntPtr handle, string methodName)
        {
            InvokeDllMethod(process, handle, methodName, Array.Empty<byte>());
        }

        public void InvokeDllMethod(Process process, IntPtr handle, string methodName, byte[] parameter);

        public void HideWindow(Process process, IntPtr handle, IntPtr hwnd)
        {
            var parameter = new HideWindowParameter() {
                hwnd = hwnd
            };

            InvokeDllMethod(process, handle, "HideWindow", parameter);
        }

        public void HideAllWindows(Process process, IntPtr handle)
        {
            InvokeDllMethod(process, handle, "HideAllWindows");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HideWindowParameter
    {
        public IntPtr hwnd;
    }
}
