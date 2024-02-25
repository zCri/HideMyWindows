using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Services.WindowHider
{
    public interface IWindowHider
    {
        public void ApplyAction(WindowHiderAction action, Process process);
        public void ApplyAction(WindowHiderAction action, IntPtr hwnd);
    }

    public enum WindowHiderAction
    {
        HideProcess, //TODO: Could implement single window hiding at a later time TODO2: TEST IT
        HideWindow
    }

    public class WindowHiderOptions
    {
        public bool FollowChildProcesses { get; set; } = false;
    }
}
