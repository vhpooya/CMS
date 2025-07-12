using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteDesktopApp.Services;

namespace RemoteDesktopApp.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IUserService _userService;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(IUserService userService, ILogger<NotificationHub> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                await _userService.UpdateUserOnlineStatusAsync(userId.Value, true);
                
                _logger.LogInformation("User {UserId} connected to notification hub", userId);
                
                // Notify other users that this user is online
                await Clients.Others.SendAsync("UserOnline", new { userId = userId.Value });
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                await _userService.UpdateUserOnlineStatusAsync(userId.Value, false);
                
                _logger.LogInformation("User {UserId} disconnected from notification hub", userId);
                
                // Notify other users that this user is offline
                await Clients.Others.SendAsync("UserOffline", new { userId = userId.Value });
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Connection {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Connection {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
        }

        public async Task JoinUserGroup(int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue || currentUserId.Value != userId)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            _logger.LogInformation("User {UserId} joined their user group", userId);
        }

        public async Task SendNotificationToUser(int targetUserId, string type, string title, string message)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return;

            try
            {
                await Clients.Group($"User_{targetUserId}").SendAsync("ReceiveNotification", new
                {
                    type = type,
                    title = title,
                    message = message,
                    fromUserId = userId.Value,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Notification sent from user {FromUserId} to user {ToUserId}", userId, targetUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification from user {FromUserId} to user {ToUserId}", userId, targetUserId);
            }
        }

        public async Task SendPhoneCallNotification(int targetUserId, string callerName, string callerPhoneNumber, int callId)
        {
            try
            {
                await Clients.Group($"User_{targetUserId}").SendAsync("IncomingCall", new
                {
                    callId = callId,
                    callerName = callerName,
                    callerPhoneNumber = callerPhoneNumber,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Incoming call notification sent to user {UserId} from {CallerName}", targetUserId, callerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending call notification to user {UserId}", targetUserId);
            }
        }

        public async Task SendSmsNotification(int targetUserId, string senderName, string senderPhoneNumber, string message)
        {
            try
            {
                await Clients.Group($"User_{targetUserId}").SendAsync("NewSms", new
                {
                    senderName = senderName,
                    senderPhoneNumber = senderPhoneNumber,
                    message = message.Length > 50 ? message.Substring(0, 50) + "..." : message,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("SMS notification sent to user {UserId} from {SenderName}", targetUserId, senderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS notification to user {UserId}", targetUserId);
            }
        }

        public async Task UpdatePhoneStatus(bool isOnline)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return;

            try
            {
                // Update phone online status in database would go here
                
                // Notify other users about phone status change
                await Clients.Others.SendAsync("PhoneStatusChanged", new
                {
                    userId = userId.Value,
                    isOnline = isOnline,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Phone status updated for user {UserId}: {Status}", userId, isOnline ? "Online" : "Offline");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating phone status for user {UserId}", userId);
            }
        }

        public async Task SendTypingIndicator(int targetUserId, bool isTyping)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return;

            try
            {
                await Clients.Group($"User_{targetUserId}").SendAsync("TypingIndicator", new
                {
                    fromUserId = userId.Value,
                    isTyping = isTyping,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending typing indicator from user {FromUserId} to user {ToUserId}", userId, targetUserId);
            }
        }

        public async Task BroadcastSystemNotification(string title, string message, string type = "info")
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return;

            // Only allow admins to broadcast system notifications
            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user?.Role != Models.UserRole.Admin)
                return;

            try
            {
                await Clients.All.SendAsync("SystemNotification", new
                {
                    title = title,
                    message = message,
                    type = type,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("System notification broadcasted by admin {UserId}: {Title}", userId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting system notification by user {UserId}", userId);
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
}
