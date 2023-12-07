using HideMyWindows.App.Services.ConfigProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Services.ProcessWatcher
{
    public partial class WMIInstanceEventProcessWatcher : IProcessWatcher
    {
        private NotificationsService NotificationsService { get; }
        private IConfigProvider ConfigProvider { get; }

        private readonly ManagementEventWatcher startWatcher = new();
        private readonly ManagementEventWatcher stopWatcher = new();

        public event EventHandler<ProcessWatchedEventArgs> ProcessStarted;
        public event EventHandler<ProcessWatchedEventArgs> ProcessStopped;

        public bool IsWatching { get; private set; } = false;

        protected virtual void OnProcessStarted(ProcessWatchedEventArgs e)
        {
            ProcessStarted?.Invoke(this, e);
        }

        protected virtual void OnProcessStopped(ProcessWatchedEventArgs e)
        {
            ProcessStopped?.Invoke(this, e);
        }

        public WMIInstanceEventProcessWatcher(NotificationsService notificationsService, IConfigProvider configProvider)
        {
            NotificationsService = notificationsService;
            ConfigProvider = configProvider;

            configProvider.Load();

            if (configProvider.Config is not null)
            {
                configProvider.Config.PropertyChanged += OnConfigChanged;
                StartQueries(configProvider.Config.WMIInstanceEventProcessWatcherTimeoutMillis);
            } else
            {
                StartQueries(1000);
            }
        }

        private void OnConfigChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(ConfigProvider.Config.WMIInstanceEventProcessWatcherTimeoutMillis))
                StartQueries(ConfigProvider.Config!.WMIInstanceEventProcessWatcherTimeoutMillis);
        }

        private void StartQueries(int intervalMillis)
        {
            startWatcher.Stop();
            stopWatcher.Stop();

            var startQuery = new WqlEventQuery()
            {
                EventClassName = "__InstanceCreationEvent",
                Condition = "TargetInstance ISA 'Win32_Process'",
                WithinInterval = TimeSpan.FromMilliseconds(intervalMillis)
            };

            startWatcher.Query = startQuery;

            var stopQuery = new WqlEventQuery()
            {
                EventClassName = "__InstanceDeletionEvent",
                Condition = "TargetInstance ISA 'Win32_Process'",
                WithinInterval = TimeSpan.FromMilliseconds(intervalMillis)
            };

            stopWatcher.Query = stopQuery;

            startWatcher.EventArrived += WMIStartEventArrived;
            stopWatcher.EventArrived += WMIStopEventArrived;

            startWatcher.Start();
            stopWatcher.Start();

            IsWatching = true;
        }

        private ProcessWatchedEventArgs GetEventArgs(EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject) e.NewEvent.Properties["TargetInstance"].Value;

            return new ProcessWatchedEventArgs
            {
                Name = (string) instance.Properties["Name"].Value,
                Id = Convert.ToInt32(instance.Properties["ProcessId"].Value)
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
