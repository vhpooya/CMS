using System.Drawing;

namespace RemoteDesktopApp.Services
{
    public interface IScreenCaptureService
    {
        /// <summary>
        /// Captures the current screen and returns it as a byte array (JPEG format)
        /// </summary>
        /// <param name="quality">JPEG quality (1-100)</param>
        /// <returns>Screen capture as byte array</returns>
        Task<byte[]> CaptureScreenAsync(int quality = 85);
        
        /// <summary>
        /// Captures a specific region of the screen
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <param name="quality">JPEG quality (1-100)</param>
        /// <returns>Screen capture as byte array</returns>
        Task<byte[]> CaptureRegionAsync(int x, int y, int width, int height, int quality = 85);
        
        /// <summary>
        /// Gets the screen dimensions
        /// </summary>
        /// <returns>Screen size</returns>
        Size GetScreenSize();
        
        /// <summary>
        /// Gets information about all available monitors
        /// </summary>
        /// <returns>List of monitor information</returns>
        List<MonitorInfo> GetMonitors();
        
        /// <summary>
        /// Captures a specific monitor
        /// </summary>
        /// <param name="monitorIndex">Index of the monitor to capture</param>
        /// <param name="quality">JPEG quality (1-100)</param>
        /// <returns>Screen capture as byte array</returns>
        Task<byte[]> CaptureMonitorAsync(int monitorIndex, int quality = 85);
    }
    
    public class MonitorInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public Rectangle Bounds { get; set; }
        public bool IsPrimary { get; set; }
    }
}
