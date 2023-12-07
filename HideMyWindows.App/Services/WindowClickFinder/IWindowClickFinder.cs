using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Services.WindowClickFinder
{
    public class WindowInfo
    {
        public IntPtr Handle { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Class { get; init; } = string.Empty;
        public Process? Process { get; init; }
    }
    
    public interface IWindowClickFinder
    {
        public Task<WindowInfo> FindWindowByClick();
    }
}
