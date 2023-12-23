using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Helpers
{
    static class ProcessExtensions
    {
        public static string TryGetProcessNameWithExtension(this Process process)
        {
            try
            {
                return Path.GetFileName(process.MainModule?.FileName) ?? process.ProcessName;
            } catch (Exception e) when (e is Win32Exception or InvalidOperationException)
            {
                return process.ProcessName;
            }
        }
    }
}
