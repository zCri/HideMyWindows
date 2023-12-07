using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace HideMyWindows.App.Services.ProcessWatcher
{
    public class WMIProcessTraceProcessWatcher : IProcessWatcher
    {
        private NotificationsService NotificationsService { get; }

        private readonly ManagementEventWatcher startWatcher;
        private readonly ManagementEventWatcher stopWatcher;

        public event EventHandler<ProcessWatchedEventArgs>? ProcessStarted;
        public event EventHandler<ProcessWatchedEventArgs>? ProcessStopped;

        public bool IsWatching { get; private set; } = false;

        protected virtual void OnProcessStarted(ProcessWatchedEventArgs e)
        {
            ProcessStarted?.Invoke(this, e);
        }

        protected virtual void OnProcessStopped(ProcessWatchedEventArgs e)
        {
            ProcessStopped?.Invoke(this, e);
        }

        public WMIProcessTraceProcessWatcher(NotificationsService notificationsService)
        {
            NotificationsService = notificationsService;

            var startQuery = new WqlEventQuery()
            {
                EventClassName = "Win32_ProcessStartTrace"
            };

            startWatcher = new ManagementEventWatcher(
                startQuery
            );

            var stopQuery = new WqlEventQuery()
            {
                EventClassName = "Win32_ProcessStopTrace"
            };

            stopWatcher = new ManagementEventWatcher(
                stopQuery
            );

            startWatcher.EventArrived += WMIStartEventArrived;
            stopWatcher.EventArrived += WMIStopEventArrived;

            try
            {
                startWatcher.Start();
                stopWatcher.Start();
                IsWatching = true;
            }
            catch (ManagementException)
            {
                NotificationsService.AddNotification("Missing permissions", "Need admin permissions to use WMI Process Trace process watcher.", Wpf.Ui.Controls.InfoBarSeverity.Warning);
                // Not running as admin
            }
        }

        private ProcessWatchedEventArgs GetEventArgs(EventArrivedEventArgs e)
        {
            return new ProcessWatchedEventArgs
            {
                Name = (string) e.NewEvent.Properties["ProcessName"].Value,
                Id = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value)
            };
        }

        private void WMIStartEventArrived(object sender, EventArrivedEventArgs e)
        {
            OnProcessStarted(GetEventArgs(e));
        }

        private void WMIStopEventArrived(object sender, EventArrivedEventArgs e)
        {
            OnProcessStopped(GetEventArgs(e));
        }
    }
}
