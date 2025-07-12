using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteDesktopApp.Models;
using RemoteDesktopApp.Services;

namespace RemoteDesktopApp.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardApiController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IMessagingService _messagingService;
        private readonly IMobilePhoneService _mobilePhoneService;
        private readonly ILogger<DashboardApiController> _logger;

        public DashboardApiController(
            IUserService userService,
            IMessagingService messagingService,
            IMobilePhoneService mobilePhoneService,
            ILogger<DashboardApiController> logger)
        {
            _userService = userService;
            _messagingService = messagingService;
            _mobilePhoneService = mobilePhoneService;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStats>> GetStats()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var stats = new DashboardStats
                {
                    TodayEvents = 3, // Mock data - replace with actual calendar service
                    TotalSpreadsheets = 12, // Mock data - replace with actual spreadsheet service
                    CodeProjects = 8, // Mock data - replace with actual code editor service
                    UnreadMessages = await GetUnreadMessagesCount(userId.Value),
                    MissedCalls = await _mobilePhoneService.GetMissedCallsCountAsync(userId.Value),
                    UnreadSms = await _mobilePhoneService.GetUnreadSmsCountAsync(userId.Value),
                    OnlineUsers = await GetOnlineUsersCount(),
                    TotalUsers = await GetTotalUsersCount()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats for user {UserId}", userId);
                return StatusCode(500, new { message = "Error loading dashboard stats" });
            }
        }

        [HttpGet("recent-activity")]
        public async Task<ActionResult<List<RecentActivity>>> GetRecentActivity()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var activities = new List<RecentActivity>
                {
                    new RecentActivity
                    {
                        Type = "call",
                        Description = "Missed call from Demo User",
                        Time = DateTime.UtcNow.AddMinutes(-15),
                        Icon = "fas fa-phone-slash",
                        Color = "text-danger"
                    },
                    new RecentActivity
                    {
                        Type = "sms",
                        Description = "New message from Alice Johnson",
                        Time = DateTime.UtcNow.AddMinutes(-30),
                        Icon = "fas fa-sms",
                        Color = "text-info"
                    },
                    new RecentActivity
                    {
                        Type = "login",
                        Description = "Logged in from new device",
                        Time = DateTime.UtcNow.AddHours(-2),
                        Icon = "fas fa-sign-in-alt",
                        Color = "text-success"
                    }
                };

                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activity for user {UserId}", userId);
                return StatusCode(500, new { message = "Error loading recent activity" });
            }
        }

        [HttpGet("upcoming-events")]
        public async Task<ActionResult<List<UpcomingEvent>>> GetUpcomingEvents()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var events = new List<UpcomingEvent>
                {
                    new UpcomingEvent
                    {
                        Title = "Team Meeting",
                        Time = DateTime.UtcNow.AddHours(2),
                        Type = "meeting",
                        Color = "primary"
                    },
                    new UpcomingEvent
                    {
                        Title = "Project Deadline",
                        Time = DateTime.UtcNow.AddDays(1),
                        Type = "deadline",
                        Color = "warning"
                    },
                    new UpcomingEvent
                    {
                        Title = "System Maintenance",
                        Time = DateTime.UtcNow.AddDays(3),
                        Type = "maintenance",
                        Color = "info"
                    }
                };

                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming events for user {UserId}", userId);
                return StatusCode(500, new { message = "Error loading upcoming events" });
            }
        }

        private async Task<int> GetUnreadMessagesCount(int userId)
        {
            try
            {
                // This would be replaced with actual messaging service call
                return 5; // Mock data
            }
            catch
            {
                return 0;
            }
        }

        private async Task<int> GetOnlineUsersCount()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(1);
                return users.Count(u => u.IsOnline);
            }
            catch
            {
                return 0;
            }
        }

        private async Task<int> GetTotalUsersCount()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(1);
                return users.Count;
            }
            catch
            {
                return 0;
            }
        }

        [HttpPut("profile")]
        public async Task<ActionResult<User>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var updatedUser = await _userService.UpdateUserProfileAsync(
                    userId.Value,
                    request.DisplayName,
                    request.Email,
                    request.Bio,
                    request.Department,
                    request.JobTitle,
                    request.PhoneNumber
                );

                if (updatedUser == null)
                    return NotFound(new { message = "User not found" });

                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return StatusCode(500, new { message = "Error updating profile" });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    public class DashboardStats
    {
        public int TodayEvents { get; set; }
        public int TotalSpreadsheets { get; set; }
        public int CodeProjects { get; set; }
        public int UnreadMessages { get; set; }
        public int MissedCalls { get; set; }
        public int UnreadSms { get; set; }
        public int OnlineUsers { get; set; }
        public int TotalUsers { get; set; }
    }

    public class RecentActivity
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class UpcomingEvent
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? Bio { get; set; }
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
