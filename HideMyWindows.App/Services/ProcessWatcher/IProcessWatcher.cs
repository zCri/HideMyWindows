using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Services.ProcessWatcher
{
    public class ProcessWatchedEventArgs : EventArgs
    {
        public string Name { get; init; } = string.Empty;
        public int Id { get; init; }
    }

    public interface IProcessWatcher
    {
        public event EventHandler<ProcessWatchedEventArgs> ProcessStarted;
        public event EventHandler<ProcessWatchedEventArgs> ProcessStopped;

        public bool IsWatching { get; }
    }

    public enum ProcessWatcherType
    {
        WMIInstanceEventProcessWatcher,
        WMIProcessTraceProcessWatcher
    }
}
