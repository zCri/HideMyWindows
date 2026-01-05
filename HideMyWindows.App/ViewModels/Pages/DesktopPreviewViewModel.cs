using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Services.DesktopPreview;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;


namespace HideMyWindows.App.ViewModels.Pages
{
    public partial class DesktopPreviewViewModel : ObservableObject
    {
        private IConfigProvider ConfigProvider { get; }
        private IDesktopPreviewService DesktopPreviewService { get; }

        public DesktopPreviewViewModel(IDesktopPreviewService desktopPreviewService, IConfigProvider configProvider) {
            DesktopPreviewService = desktopPreviewService;
            ConfigProvider = configProvider;
        }

        [ObservableProperty]
        private BitmapSource? _previewImage;

        [ObservableProperty]
        private BindingList<IntPtr> _availableMonitors = new();

        [ObservableProperty]
        private IntPtr? selectedMonitor;

        public void ResetSelectedMonitor() // Hacky way to refresh combobox binding converter
        {
            if (AvailableMonitors.Count != 0)
            {
                SelectedMonitor = null;
                SelectedMonitor = AvailableMonitors.First();
            }
        }

        public void StartCapture()
        {
            if (AvailableMonitors.Count == 0)
            {
                Debug.WriteLine("[DashboardViewModel] EnumDisplayMonitors started...");
                EnumDisplayMonitors(HDC.NULL, null, (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
                {
                    AvailableMonitors.Add(hMonitor);
                    return true;
                }, IntPtr.Zero);

                Debug.WriteLine($"[DashboardViewModel] Found {AvailableMonitors.Count} monitors.");
            }

            if (AvailableMonitors.Count != 0 && SelectedMonitor is IntPtr handle)
            {
                try
                {
                    Debug.WriteLine($"[DashboardViewModel] Starting capture for handle: {handle}");
                    DesktopPreviewService.FrameCaptured += OnFrameCaptured;
                    DesktopPreviewService.StartCapture(handle);
                }
                catch (Exception ex)
                {
                    // Failed to start
                    Debug.WriteLine($"[DashboardViewModel] StartCapture failed: {ex}");
                }
            }
            else
            {
                Debug.WriteLine("[DashboardViewModel] No monitors found!");
            }
        }

        public void StopCapture()
        {
            Debug.WriteLine("[DashboardViewModel] Stopping capture.");
            DesktopPreviewService.StopCapture();
            DesktopPreviewService.FrameCaptured -= OnFrameCaptured;
            PreviewImage = null;

            // Clear monitors list when preview is disabled
            AvailableMonitors.Clear();
        }

        partial void OnSelectedMonitorChanged(IntPtr? value)
        {
            StartCapture(); // Restart with new monitor
        }

        private void OnFrameCaptured(object? sender, System.Windows.Media.Imaging.BitmapSource e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                PreviewImage = e;
            });
        }
    }
}
