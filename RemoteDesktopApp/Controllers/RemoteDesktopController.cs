using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteDesktopApp.Services;
using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RemoteDesktopController : ControllerBase
    {
        private readonly IScreenCaptureService _screenCaptureService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RemoteDesktopController> _logger;

        public RemoteDesktopController(
            IScreenCaptureService screenCaptureService,
            IConfiguration configuration,
            ILogger<RemoteDesktopController> logger)
        {
            _screenCaptureService = screenCaptureService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("screen-info")]
        public async Task<ActionResult> GetScreenInfo()
        {
            try
            {
                var screenSize = _screenCaptureService.GetScreenSize();
                var monitors = _screenCaptureService.GetMonitors();

                return Ok(new
                {
                    ScreenSize = screenSize,
                    Monitors = monitors,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting screen info");
                return StatusCode(500, new { error = "Failed to get screen information" });
            }
        }

        [HttpGet("screenshot")]
        public async Task<ActionResult> GetScreenshot([FromQuery] int quality = 85)
        {
            try
            {
                var screenData = await _screenCaptureService.CaptureScreenAsync(quality);
                return File(screenData, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing screenshot");
                return StatusCode(500, new { error = "Failed to capture screenshot" });
            }
        }

        [HttpGet("screenshot/region")]
        public async Task<ActionResult> GetRegionScreenshot(
            [FromQuery] int x,
            [FromQuery] int y,
            [FromQuery] int width,
            [FromQuery] int height,
            [FromQuery] int quality = 85)
        {
            try
            {
                var screenData = await _screenCaptureService.CaptureRegionAsync(x, y, width, height, quality);
                return File(screenData, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing region screenshot");
                return StatusCode(500, new { error = "Failed to capture region screenshot" });
            }
        }

        [HttpGet("screenshot/monitor/{monitorIndex}")]
        public async Task<ActionResult> GetMonitorScreenshot(int monitorIndex, [FromQuery] int quality = 85)
        {
            try
            {
                var screenData = await _screenCaptureService.CaptureMonitorAsync(monitorIndex, quality);
                return File(screenData, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing monitor screenshot");
                return StatusCode(500, new { error = "Failed to capture monitor screenshot" });
            }
        }

        [HttpGet("settings")]
        public async Task<ActionResult<RemoteDesktopSettings>> GetSettings()
        {
            try
            {
                var settings = new RemoteDesktopSettings
                {
                    MaxConnections = _configuration.GetValue<int>("RemoteDesktop:MaxConnections", 10),
                    ScreenCaptureQuality = _configuration.GetValue<int>("RemoteDesktop:ScreenCaptureQuality", 85),
                    FrameRate = _configuration.GetValue<int>("RemoteDesktop:FrameRate", 30),
                    EnableFileTransfer = _configuration.GetValue<bool>("RemoteDesktop:EnableFileTransfer", true),
                    EnableClipboardSync = _configuration.GetValue<bool>("RemoteDesktop:EnableClipboardSync", true),
                    RequireAuthentication = true
                };

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting settings");
                return StatusCode(500, new { error = "Failed to get settings" });
            }
        }

        [HttpGet("status")]
        public async Task<ActionResult> GetStatus()
        {
            try
            {
                return Ok(new
                {
                    Status = "Online",
                    Version = "1.0.0",
                    Uptime = DateTime.UtcNow.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()),
                    ActiveConnections = 0, // This would be tracked in a real implementation
                    ServerTime = DateTime.UtcNow,
                    Features = new
                    {
                        ScreenCapture = true,
                        InputControl = true,
                        FileTransfer = _configuration.GetValue<bool>("RemoteDesktop:EnableFileTransfer", true),
                        ClipboardSync = _configuration.GetValue<bool>("RemoteDesktop:EnableClipboardSync", true),
                        MultiMonitor = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status");
                return StatusCode(500, new { error = "Failed to get status" });
            }
        }
    }
}
