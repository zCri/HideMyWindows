using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace HideMyWindows.App.Services.DllInjector
{
    public class LoadLibraryDllInjector : IDllInjector
    {
        public readonly static string DLLName64 = "HideMyWindows.DLL.x64.dll";
        public readonly static string DLLName32 = "HideMyWindows.DLL.Win32.dll";

        // TODO: Hide from tray ?
        public void InjectDll(Process process)
        {
            var DLLPath = Path.Combine(Directory.GetCurrentDirectory(), IsProcess64Bit(process) ? DLLName64 : DLLName32);

            var memSize = Encoding.Unicode.GetByteCount(DLLPath);
            var mem = VirtualAllocEx(process.Handle, IntPtr.Zero, memSize, MEM_ALLOCATION_TYPE.MEM_RESERVE | MEM_ALLOCATION_TYPE.MEM_COMMIT, MEM_PROTECTION.PAGE_READWRITE);
            if (mem == IntPtr.Zero) Win32Error.ThrowLastError();

            if (!WriteProcessMemory(process.Handle, mem, Encoding.Unicode.GetBytes(DLLPath), memSize, out _)) Win32Error.ThrowLastError();

            var loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW"); // Critical system libraries are always initialized at the same address
            if (loadLibraryAddr == IntPtr.Zero) Win32Error.ThrowLastError();

            var thread = CreateRemoteThread(process.Handle, null, 0, loadLibraryAddr, mem, CREATE_THREAD_FLAGS.RUN_IMMEDIATELY, out _);
            if (thread == IntPtr.Zero) Win32Error.ThrowLastError();
        }

        private bool IsProcess64Bit(Process process)
        {
            if (!Environment.Is64BitOperatingSystem)
                return false;

            if (!IsWow64Process(process.Handle, out bool isWow64Process))
                Win32Error.ThrowLastError();

            return !isWow64Process;
        }
    }
}
