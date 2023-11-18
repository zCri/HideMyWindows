using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace HideMyWindows.App.Services
{
    public class NotificationsService
    {
        public BindingList<InfoBar> Notifications { get; } = new BindingList<InfoBar>();

        public InfoBar AddNotification(string title, string message, InfoBarSeverity severity)
        {
            var infoBar = new InfoBar()
            {
                Title = title,
                Message = message,
                Severity = severity,
                IsOpen = true,
                IsClosable = false
            };

            Notifications.Add(infoBar);
            return infoBar;
        }

        public InfoBar AddNotification(string title, string message, InfoBarSeverity severity, int timeoutMillis)
        {
            var infoBar = AddNotification(title, message, severity);

            Task.Delay(timeoutMillis).ContinueWith(_ =>
            {
                Notifications.Remove(infoBar);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            return infoBar;
        }
    }
}
