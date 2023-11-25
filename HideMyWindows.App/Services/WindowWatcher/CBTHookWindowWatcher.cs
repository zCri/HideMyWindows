using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace HideMyWindows.App.Services.WindowWatcher
{
    public class UIAutomationWindowWatcher : IWindowWatcher
    {
        public event EventHandler<WindowWatchedEventArgs> WindowCreated;
        public event EventHandler<WindowWatchedEventArgs> WindowDestroyed;

        public bool IsWatching { get; private set; } = false;

        protected virtual void OnWindowCreated(WindowWatchedEventArgs e)
        {
            WindowCreated?.Invoke(this, e);
        }

        protected virtual void OnWindowDestroyed(WindowWatchedEventArgs e)
        {
            WindowDestroyed?.Invoke(this, e);
        }

        public UIAutomationWindowWatcher()
        {
            Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, TreeScope.Descendants, AutomationCreatedEventArrived);
            Automation.AddAutomationEventHandler(WindowPattern.WindowClosedEvent, AutomationElement.RootElement, TreeScope.Descendants, AutomationDestroyedEventArrived);
        }

        private WindowWatchedEventArgs GetEventArgs(AutomationElement element)
        {
            return new WindowWatchedEventArgs
            {
                Handle = new IntPtr(element.Current.NativeWindowHandle),
                Title = element.Current.Name,
                Class = element.Current.ClassName
            };
        }

        private void AutomationCreatedEventArrived(object sender, AutomationEventArgs e)
        {
            if(sender is AutomationElement element)
            {
                OnWindowCreated(GetEventArgs(element));
            }
        }

        private void AutomationDestroyedEventArrived(object sender, AutomationEventArgs e)
        {
            if(sender is AutomationElement element)
            {
                OnWindowDestroyed(GetEventArgs(element));
            }
        }
    }
}
