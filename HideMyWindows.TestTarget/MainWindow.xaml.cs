using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace HideMyWindows.TestTarget
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        public static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_MONITOR = 0x00000001;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += _timer_Tick;
            _timer.Start();
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            if (ChkPersistent.IsChecked == true)
            {
                SetAffinity(WDA_EXCLUDEFROMCAPTURE);
            }
        }

        private void SetAffinity(uint affinity)
        {
            var helper = new WindowInteropHelper(this);
            bool result = SetWindowDisplayAffinity(helper.Handle, affinity);
            
            if (affinity == WDA_EXCLUDEFROMCAPTURE)
                StatusText.Text = $"Status: Hidden (Affinity 0x{affinity:X}) - Result: {result}";
            else
                StatusText.Text = $"Status: Visible (Affinity 0x{affinity:X}) - Result: {result}";
        }

        private void BtnHide_Click(object sender, RoutedEventArgs e)
        {
            SetAffinity(WDA_EXCLUDEFROMCAPTURE);
        }

        private void BtnShow_Click(object sender, RoutedEventArgs e)
        {
            SetAffinity(WDA_NONE);
        }
    }
}
