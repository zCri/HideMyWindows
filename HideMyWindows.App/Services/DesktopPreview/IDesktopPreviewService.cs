using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace HideMyWindows.App.Services.DesktopPreview
{
    public interface IDesktopPreviewService
    {
        public event EventHandler<BitmapSource>? FrameCaptured;

        public void StartCapture(IntPtr hmon);
        public void StopCapture();
    }
}
