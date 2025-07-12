using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RemoteDesktopApp.Services
{
    public class ScreenCaptureService : IScreenCaptureService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hGDIObj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private const int SRCCOPY = 0x00CC0020;
        private const uint MONITORINFOF_PRIMARY = 0x00000001;

        public async Task<byte[]> CaptureScreenAsync(int quality = 85)
        {
            return await Task.Run(() =>
            {
                var screenSize = GetScreenSize();
                return CaptureRegion(0, 0, screenSize.Width, screenSize.Height, quality);
            });
        }

        public async Task<byte[]> CaptureRegionAsync(int x, int y, int width, int height, int quality = 85)
        {
            return await Task.Run(() => CaptureRegion(x, y, width, height, quality));
        }

        public Size GetScreenSize()
        {
            return new Size(10, 100);
            //return new Size(
            //    System.Windows.Forms.SystemInformation.VirtualScreen.Width,
            //    System.Windows.Forms.SystemInformation.VirtualScreen.Height
            //);
        }

        public List<MonitorInfo> GetMonitors()
        {
            var monitors = new List<MonitorInfo>();
            var index = 0;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                var mi = new MONITORINFO();
                mi.cbSize = (uint)Marshal.SizeOf(mi);
                
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    monitors.Add(new MonitorInfo
                    {
                        Index = index++,
                        Name = $"Monitor {index}",
                        Bounds = new Rectangle(
                            mi.rcMonitor.Left,
                            mi.rcMonitor.Top,
                            mi.rcMonitor.Right - mi.rcMonitor.Left,
                            mi.rcMonitor.Bottom - mi.rcMonitor.Top
                        ),
                        IsPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0
                    });
                }
                return true;
            }, IntPtr.Zero);

            return monitors;
        }

        public async Task<byte[]> CaptureMonitorAsync(int monitorIndex, int quality = 85)
        {
            return await Task.Run(() =>
            {
                var monitors = GetMonitors();
                if (monitorIndex < 0 || monitorIndex >= monitors.Count)
                    throw new ArgumentOutOfRangeException(nameof(monitorIndex));

                var monitor = monitors[monitorIndex];
                return CaptureRegion(monitor.Bounds.X, monitor.Bounds.Y, monitor.Bounds.Width, monitor.Bounds.Height, quality);
            });
        }

        private byte[] CaptureRegion(int x, int y, int width, int height, int quality)
        {
            IntPtr hDesk = GetDC(IntPtr.Zero);
            IntPtr hSrce = CreateCompatibleDC(hDesk);
            IntPtr hBmp = CreateCompatibleBitmap(hDesk, width, height);
            IntPtr hOldBmp = SelectObject(hSrce, hBmp);

            try
            {
                bool result = BitBlt(hSrce, 0, 0, width, height, hDesk, x, y, SRCCOPY);
                if (!result)
                    throw new InvalidOperationException("Failed to capture screen");

                using (var bitmap = Image.FromHbitmap(hBmp))
                {
                    using (var stream = new MemoryStream())
                    {
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                        
                        var jpegCodec = ImageCodecInfo.GetImageDecoders()
                            .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                        
                        if (jpegCodec != null)
                        {
                            bitmap.Save(stream, jpegCodec, encoderParams);
                        }
                        else
                        {
                            bitmap.Save(stream, ImageFormat.Jpeg);
                        }
                        
                        return stream.ToArray();
                    }
                }
            }
            finally
            {
                SelectObject(hSrce, hOldBmp);
                DeleteObject(hBmp);
                DeleteDC(hSrce);
                ReleaseDC(IntPtr.Zero, hDesk);
            }
        }
    }
}
