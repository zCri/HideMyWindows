using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Services.ProcessWatcher
{
    public class ProcessWatchEventArgs : EventArgs
    {
        public string Name { get; set; } = string.Empty;
        public int ID { get; set; }
    }

    public interface IProcessWatcher
    {
        public event EventHandler<ProcessWatchEventArgs> ProcessStarted;
        public event EventHandler<ProcessWatchEventArgs> ProcessStopped;

        public bool IsWatching { get; set; }
    }
}
