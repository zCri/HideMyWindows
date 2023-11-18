using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace HideMyWindows.App.Services.ProcessWatcher
{
    public class WMIProcessWatcher : IProcessWatcher
    {
        private NotificationsService NotificationsService { get; }

        private readonly ManagementEventWatcher startWatcher;
        private readonly ManagementEventWatcher stopWatcher;

        public event EventHandler<ProcessWatchEventArgs> ProcessStarted;
        public event EventHandler<ProcessWatchEventArgs> ProcessStopped;

        public bool IsWatching { get; set; } = false;

        protected virtual void OnProcessStarted(ProcessWatchEventArgs e)
        {
            ProcessStarted?.Invoke(this, e);
        }

        protected virtual void OnProcessStopped(ProcessWatchEventArgs e)
        {
            ProcessStopped?.Invoke(this, e);
        }


        public WMIProcessWatcher(NotificationsService notificationsService)
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
                NotificationsService.AddNotification("t", "t", Wpf.Ui.Controls.InfoBarSeverity.Warning);
                // Not running as admin
            }
        }

        private ProcessWatchEventArgs GetEventArgs(EventArrivedEventArgs e)
        {
            return new ProcessWatchEventArgs
            {
                Name = (string)e.NewEvent.Properties["ProcessName"].Value,
                ID = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value)
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
