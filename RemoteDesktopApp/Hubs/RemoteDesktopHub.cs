using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using RemoteDesktopApp.Services;
using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Hubs
{
    [Authorize]
    public class RemoteDesktopHub : Hub
    {
        private readonly IScreenCaptureService _screenCaptureService;
        private readonly IInputService _inputService;
        private readonly ILogger<RemoteDesktopHub> _logger;
        private static readonly Dictionary<string, RemoteSession> _activeSessions = new();

        public RemoteDesktopHub(
            IScreenCaptureService screenCaptureService,
            IInputService inputService,
            ILogger<RemoteDesktopHub> logger)
        {
            _screenCaptureService = screenCaptureService;
            _inputService = inputService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            var userId = Context.User?.Identity?.Name ?? "Anonymous";
            
            _logger.LogInformation($"Client connected: {connectionId} (User: {userId})");
            
            // Create new session
            var session = new RemoteSession
            {
                ConnectionId = connectionId,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            _activeSessions[connectionId] = session;
            
            // Send initial screen info
            var screenSize = _screenCaptureService.GetScreenSize();
            var monitors = _screenCaptureService.GetMonitors();
            
            await Clients.Caller.SendAsync("ScreenInfo", new
            {
                ScreenSize = screenSize,
                Monitors = monitors
            });
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            
            if (_activeSessions.TryGetValue(connectionId, out var session))
            {
                session.IsActive = false;
                session.DisconnectedAt = DateTime.UtcNow;
                _activeSessions.Remove(connectionId);
            }
            
            _logger.LogInformation($"Client disconnected: {connectionId}");
            
            await base.OnDisconnectedAsync(exception);
        }

        public async Task RequestScreenCapture(int quality = 85)
        {
            try
            {
                var screenData = await _screenCaptureService.CaptureScreenAsync(quality);
                await Clients.Caller.SendAsync("ScreenCapture", Convert.ToBase64String(screenData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing screen");
                await Clients.Caller.SendAsync("Error", "Failed to capture screen");
            }
        }

        public async Task RequestRegionCapture(int x, int y, int width, int height, int quality = 85)
        {
            try
            {
                var screenData = await _screenCaptureService.CaptureRegionAsync(x, y, width, height, quality);
                await Clients.Caller.SendAsync("RegionCapture", Convert.ToBase64String(screenData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing screen region");
                await Clients.Caller.SendAsync("Error", "Failed to capture screen region");
            }
        }

        public async Task RequestMonitorCapture(int monitorIndex, int quality = 85)
        {
            try
            {
                var screenData = await _screenCaptureService.CaptureMonitorAsync(monitorIndex, quality);
                await Clients.Caller.SendAsync("MonitorCapture", Convert.ToBase64String(screenData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing monitor");
                await Clients.Caller.SendAsync("Error", "Failed to capture monitor");
            }
        }

        public async Task MouseClick(int x, int y, string button = "left", bool isDoubleClick = false)
        {
            try
            {
                var mouseButton = button.ToLower() switch
                {
                    "right" => MouseButton.Right,
                    "middle" => MouseButton.Middle,
                    _ => MouseButton.Left
                };
                
                _inputService.MouseClick(x, y, mouseButton, isDoubleClick);
                await Clients.Caller.SendAsync("InputAck", "MouseClick");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mouse click");
                await Clients.Caller.SendAsync("Error", "Failed to process mouse click");
            }
        }

        public async Task MouseMove(int x, int y)
        {
            try
            {
                _inputService.MouseMove(x, y);
                // Don't send ack for mouse move to avoid flooding
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mouse move");
            }
        }

        public async Task MouseDrag(int startX, int startY, int endX, int endY, string button = "left")
        {
            try
            {
                var mouseButton = button.ToLower() switch
                {
                    "right" => MouseButton.Right,
                    "middle" => MouseButton.Middle,
                    _ => MouseButton.Left
                };
                
                _inputService.MouseDrag(startX, startY, endX, endY, mouseButton);
                await Clients.Caller.SendAsync("InputAck", "MouseDrag");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mouse drag");
                await Clients.Caller.SendAsync("Error", "Failed to process mouse drag");
            }
        }

        public async Task MouseWheel(int x, int y, int delta)
        {
            try
            {
                _inputService.MouseWheel(x, y, delta);
                await Clients.Caller.SendAsync("InputAck", "MouseWheel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mouse wheel");
                await Clients.Caller.SendAsync("Error", "Failed to process mouse wheel");
            }
        }

        public async Task KeyPress(int key, bool isKeyDown)
        {
            try
            {
                _inputService.KeyPress(key, isKeyDown);
                await Clients.Caller.SendAsync("InputAck", "KeyPress");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing key press");
                await Clients.Caller.SendAsync("Error", "Failed to process key press");
            }
        }

        public async Task TypeText(string text)
        {
            try
            {
                _inputService.TypeText(text);
                await Clients.Caller.SendAsync("InputAck", "TypeText");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing text input");
                await Clients.Caller.SendAsync("Error", "Failed to process text input");
            }
        }

        public async Task KeyCombination(string modifiers, int key)
        {
            try
            {
                var modifierKeys = ModifierKeys.None;
                var modifierList = modifiers.ToLower().Split(',');
                
                foreach (var modifier in modifierList)
                {
                    switch (modifier.Trim())
                    {
                        case "ctrl":
                            modifierKeys |= ModifierKeys.Ctrl;
                            break;
                        case "alt":
                            modifierKeys |= ModifierKeys.Alt;
                            break;
                        case "shift":
                            modifierKeys |= ModifierKeys.Shift;
                            break;
                        case "win":
                        case "windows":
                            modifierKeys |= ModifierKeys.Windows;
                            break;
                    }
                }
                
                _inputService.KeyCombination(modifierKeys, key);
                await Clients.Caller.SendAsync("InputAck", "KeyCombination");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing key combination");
                await Clients.Caller.SendAsync("Error", "Failed to process key combination");
            }
        }

        public async Task GetActiveSessions()
        {
            try
            {
                var sessions = _activeSessions.Values.Where(s => s.IsActive).ToList();
                await Clients.Caller.SendAsync("ActiveSessions", sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions");
                await Clients.Caller.SendAsync("Error", "Failed to get active sessions");
            }
        }
    }
}
