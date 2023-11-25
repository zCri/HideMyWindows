using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Services.WindowWatcher
{
    public class WindowWatchedEventArgs : EventArgs
    {
        public IntPtr Handle { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Class { get; init; } = string.Empty;
    }

    public interface IWindowWatcher
    {
        public event EventHandler<WindowWatchedEventArgs> WindowCreated;
        public event EventHandler<WindowWatchedEventArgs> WindowDestroyed;
        public bool IsWatching { get; }
    }
}
