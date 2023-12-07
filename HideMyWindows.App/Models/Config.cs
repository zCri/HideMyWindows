using HideMyWindows.App.Services.ProcessWatcher;
using HideMyWindows.App.Services.WindowWatcher;
using System.ComponentModel;
using System.Windows.Interop;
using Wpf.Ui.Appearance;
using static Vanara.PInvoke.User32;

namespace HideMyWindows.App.Models
{
    public partial class Config : ObservableObject
    {
        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Dark;

        [ObservableProperty]
        private bool _hideSelf = true;

        [ObservableProperty]
        private ProcessWatcherType _processWatcherType = ProcessWatcherType.WMIProcessTraceProcessWatcher;

        [ObservableProperty]
        private WindowWatcherType _windowWatcherType = WindowWatcherType.UIAutomationWindowWatcher;

        [ObservableProperty]
        private int _WMIInstanceEventProcessWatcherTimeoutMillis = 1000;

        [ObservableProperty]
        private BindingList<WindowRule> _windowRules = new();

        partial void OnCurrentThemeChanged(ApplicationTheme value)
        {
            ApplicationThemeManager.Apply(value);
        }

        // Weird behaviour with the JSON deserialization (?), not calling the property setter if the value in the config is equal to the default, might be fixable with JsonSerializerOptions.PreferredObjectCreationHandling (?) but it's .NET 8+.
        partial void OnHideSelfChanged(bool value)
        {
            foreach (var window in Application.Current.Windows)
            {
                var interop = new WindowInteropHelper((Window) window);
                var hwnd = interop.EnsureHandle();

                SetWindowDisplayAffinity(hwnd, value ? (WindowDisplayAffinity) 0x11 : WindowDisplayAffinity.WDA_NONE);
            }
        }
    }
}
