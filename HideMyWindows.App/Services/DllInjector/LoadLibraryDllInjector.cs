using HideMyWindows.App.Helpers;
using PeNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace HideMyWindows.App.Services.DllInjector
{
    public class LoadLibraryDllInjector : IDllInjector
    {
        public readonly static string DllName64 = "HideMyWindows.DLL.x64.dll";
        public readonly static string DllName32 = "HideMyWindows.DLL.Win32.dll";

        private IntPtr? LoadLibraryWAddr32; // Critical system libraries are always initialized at the same address
        private IntPtr? LoadLibraryWAddr64; // Critical system libraries are always initialized at the same address

        private Dictionary<string, IntPtr> DllProcOffsets64 { get; }
        private Dictionary<string, IntPtr> DllProcOffsets32 { get; }

        public LoadLibraryDllInjector()
        {
            if(Environment.Is64BitProcess)
                LoadLibraryWAddr64 = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
            else
                LoadLibraryWAddr32 = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
            //LoadLibraryWAddr32 = new IntPtr(0x764ed8a0);
            if (LoadLibraryWAddr64 == IntPtr.Zero || LoadLibraryWAddr32 == IntPtr.Zero) throw GetLastError().GetException();

            var DllPath64 = Path.Combine(Directory.GetCurrentDirectory(), DllName64);
            var DllPath32 = Path.Combine(Directory.GetCurrentDirectory(), DllName32);

            DllProcOffsets64 = new();
            DllProcOffsets32 = new();

            PopulateOffsetMap(DllPath64, DllProcOffsets64);
            PopulateOffsetMap(DllPath32, DllProcOffsets32);
        }

        private void PopulateOffsetMap(string dllPath, Dictionary<string, IntPtr> offsets)
        {
            var pe = new PeFile(dllPath);
            if(pe.ExportedFunctions is not null)
            {
                foreach(var export in pe.ExportedFunctions)
                {
                    if(export.Name is not null)
                        offsets.Add(export.Name, new IntPtr(export.Address));
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            UIntPtr nSize,
            out UIntPtr lpNumberOfBytesRead
        );

        // Caution: Handle may work only on 32 bit processes
        public IntPtr InjectDll(Process process)
        {
            if (process.HasExited) return IntPtr.Zero;
            var DllPath = Path.Combine(Directory.GetCurrentDirectory(), IsProcess64Bit(process) ? DllName64 : DllName32);

            var memSize = Encoding.Unicode.GetByteCount(DllPath);
            var mem = VirtualAllocEx(process.Handle, IntPtr.Zero, memSize, MEM_ALLOCATION_TYPE.MEM_RESERVE | MEM_ALLOCATION_TYPE.MEM_COMMIT, MEM_PROTECTION.PAGE_READWRITE);
            if (mem == IntPtr.Zero) throw GetLastError().GetException();

            if (!WriteProcessMemory(process.Handle, mem, Encoding.Unicode.GetBytes(DllPath), memSize, out _)) throw GetLastError().GetException();

            ref var addr = ref IsProcess64Bit(process) ? ref LoadLibraryWAddr64 : ref LoadLibraryWAddr32;
            addr ??= FindLoadLibraryWAddr(process);

            var thread = CreateRemoteThread(process.Handle, null, 0, addr!.Value, mem, CREATE_THREAD_FLAGS.RUN_IMMEDIATELY, out _);
            if (thread == IntPtr.Zero) throw GetLastError().GetException();

            if (WaitForSingleObject(thread, INFINITE) == WAIT_STATUS.WAIT_FAILED) throw GetLastError().GetException();
            if (!GetExitCodeThread(thread, out var handle32)) throw GetLastError().GetException();

            return new IntPtr(handle32);
        }

        public IntPtr? FindLoadLibraryWAddr(Process process)
        {
            if (!IsProcess64Bit(process))
            {
                /*                    var pData = IntPtr.Zero;
                                    try
                                    {
                                        pData = Marshal.AllocHGlobal(IntPtr.Size);
                                        NtQueryInformationProcess(process.Handle, PROCESSINFOCLASS.ProcessWow64Information, pData, (uint)IntPtr.Size, out _);
                                        var pebAddr = Marshal.ReadIntPtr(pData);
                                        var pebSize = Marshal.SizeOf<PEB>();
                                        var pebBytes = new byte[pebSize];
                                        ReadProcessMemory(process.Handle, pebAddr, pebBytes, new UIntPtr((uint)Marshal.SizeOf<PEB>()), out var _);
                                    }
                                    finally
                                    {
                                        Marshal.FreeHGlobal(pData);
                                    }*/

                // Need to resume main thread to let WOW64 build the PEB32 and load the modules (presumably?)
                foreach (var sThreadObj in process.Threads)
                {
                    if (sThreadObj is ProcessThread sThread)
                    {
                        if (sThread.ThreadState == System.Diagnostics.ThreadState.Wait && sThread.WaitReason == ThreadWaitReason.Suspended)
                        {
                            var hThread = OpenThread(0x0002, false, (uint)sThread.Id); // THREAD_SUSPEND_RESUME
                            if (hThread.IsNull) throw GetLastError().GetException();

                            ResumeThread(hThread);
                        }
                    }
                }

                /*                var modules = EnumProcessModulesEx(process.Handle, LIST_MODULES.LIST_MODULES_32BIT);
                                foreach (var module in modules)
                                {
                                    var pathBuilder = new StringBuilder(MAX_PATH);
                                    if (GetModuleFileNameEx(process.Handle, module, pathBuilder, MAX_PATH) == 0) throw GetLastError().GetException();

                                    var path = pathBuilder.ToString();
                                    if (false && Path.GetFileName(path).ToLower() == "kernel32.dll")
                                    {
                                        if (!GetModuleInformation(process.Handle, module, out var moduleInfo, (uint)Marshal.SizeOf<MODULEINFO>())) throw GetLastError().GetException();

                                        var pe = new PeFile(path);
                                        if (pe.ExportedFunctions is not null)
                                        {
                                            foreach (var export in pe.ExportedFunctions)
                                            {
                                                if (export.Name == "LoadLibraryW")
                                                {
                                                    ;
                                                    //return moduleInfo.lpBaseOfDll.Increment(export.Address);
                                                }
                                            }
                                        }
                                    }
                                }*/

                // Sleeping and retrying to give the process enough time to load the modules
                int count = 0;
                bool moduleQuerySucceeded = false;
                SafeHSNAPSHOT snapshot;
                var module = MODULEENTRY32.Default;
                do
                {
                    snapshot = CreateToolhelp32Snapshot(TH32CS.TH32CS_SNAPMODULE | TH32CS.TH32CS_SNAPMODULE32, (uint)process.Id);
                    if (snapshot.IsInvalid) GetLastError().ThrowUnless(Win32Error.ERROR_PARTIAL_COPY);
                    else
                    {
                        count = 0;
                        moduleQuerySucceeded = Module32First(snapshot, ref module);
                        if (!moduleQuerySucceeded) GetLastError().ThrowUnless(Win32Error.ERROR_NO_MORE_FILES);
                    }
                    Sleep(20);
                } while ((snapshot.IsInvalid || !moduleQuerySucceeded) && count++ < 5);


                do
                {
                    if (Path.GetFileName(module.szExePath).ToLower() == "kernel32.dll")
                    {
                        var pe = new PeFile(module.szExePath);
                        if (pe.ExportedFunctions is not null)
                        {
                            foreach (var export in pe.ExportedFunctions)
                            {
                                if (export.Name == "LoadLibraryW")
                                {
                                    return module.modBaseAddr.Increment(export.Address);
                                }
                            }
                        }
                    }
                } while (Module32Next(snapshot, ref module));

                /*                    var snapshot = CreateToolhelp32Snapshot(TH32CS.TH32CS_SNAPMODULE | TH32CS.TH32CS_SNAPMODULE32, (uint)process.Id);
                                    if (snapshot.IsInvalid) throw GetLastError().GetException();

                                    var module = MODULEENTRY32.Default;
                                    if (!Module32First(snapshot, ref module)) throw GetLastError().GetException();

                                    do
                                    {
                                        ;
                                    } while (Module32Next(snapshot, ref module));*/
            } else
            {
                throw new NotImplementedException("Injecting into 64 bit processes from a 32 bit process is currently not supported.");
            }

            return null;
        }

        public void InvokeDllMethod(Process process, IntPtr handle, string methodName, byte[] parameter)
        {
            var DllPath = Path.Combine(Directory.GetCurrentDirectory(), IsProcess64Bit(process) ? DllName64 : DllName32);

            if (!IsProcess64Bit(process))
            {
                var memSize = parameter.Length;

                if (memSize > 0)
                {
                    var mem = VirtualAllocEx(process.Handle, IntPtr.Zero, memSize, MEM_ALLOCATION_TYPE.MEM_RESERVE | MEM_ALLOCATION_TYPE.MEM_COMMIT, MEM_PROTECTION.PAGE_READWRITE);
                    if (mem == IntPtr.Zero) throw GetLastError().GetException();

                    if (!WriteProcessMemory(process.Handle, mem, parameter, memSize, out _)) throw GetLastError().GetException();

                    var thread = CreateRemoteThread(process.Handle, null, 0, (IsProcess64Bit(process) ? DllProcOffsets64 : DllProcOffsets32)[methodName].Increment(handle), mem, CREATE_THREAD_FLAGS.RUN_IMMEDIATELY, out _);
                    if (thread == IntPtr.Zero) throw GetLastError().GetException();
                }
                else
                {
                    var thread = CreateRemoteThread(process.Handle, null, 0, (IsProcess64Bit(process) ? DllProcOffsets64 : DllProcOffsets32)[methodName].Increment(handle), IntPtr.Zero, CREATE_THREAD_FLAGS.RUN_IMMEDIATELY, out _);
                    if (thread == IntPtr.Zero) throw GetLastError().GetException();
                }
            }
            else
            {
                foreach (var obj in process.Modules)
                {
                    if (obj is ProcessModule module)
                    {
                        if (module.FileName == DllPath)
                        {
                            var memSize = parameter.Length;

                            if (memSize > 0)
                            {
                                var mem = VirtualAllocEx(process.Handle, IntPtr.Zero, memSize, MEM_ALLOCATION_TYPE.MEM_RESERVE | MEM_ALLOCATION_TYPE.MEM_COMMIT, MEM_PROTECTION.PAGE_READWRITE);
                                if (mem == IntPtr.Zero) throw GetLastError().GetException();

                                if (!WriteProcessMemory(process.Handle, mem, parameter, memSize, out _)) throw GetLastError().GetException();

                                var thread = CreateRemoteThread(process.Handle, null, 0, (IsProcess64Bit(process) ? DllProcOffsets64 : DllProcOffsets32)[methodName].Increment(module.BaseAddress), mem, CREATE_THREAD_FLAGS.RUN_IMMEDIATELY, out _);
                                if (thread == IntPtr.Zero) throw GetLastError().GetException();
                            }
                            else
                            {
                                var thread = CreateRemoteThread(process.Handle, null, 0, (IsProcess64Bit(process) ? DllProcOffsets64 : DllProcOffsets32)[methodName].Increment(module.BaseAddress), IntPtr.Zero, CREATE_THREAD_FLAGS.RUN_IMMEDIATELY, out _);
                                if (thread == IntPtr.Zero) throw GetLastError().GetException();
                            }

                            return;
                        }
                    }
                }
            }
        }

        private bool IsProcess64Bit(Process process)
        {
            if (!Environment.Is64BitOperatingSystem)
                return false;

            if (!IsWow64Process(process.Handle, out bool isWow64Process))
                throw GetLastError().GetException();

            return !isWow64Process;
        }
    }
}
