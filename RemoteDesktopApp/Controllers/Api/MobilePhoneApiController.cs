using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteDesktopApp.Models;
using RemoteDesktopApp.Services;

namespace RemoteDesktopApp.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/mobilephone")]
    public class MobilePhoneApiController : ControllerBase
    {
        private readonly IMobilePhoneService _mobilePhoneService;
        private readonly ILogger<MobilePhoneApiController> _logger;

        public MobilePhoneApiController(IMobilePhoneService mobilePhoneService, ILogger<MobilePhoneApiController> logger)
        {
            _mobilePhoneService = mobilePhoneService;
            _logger = logger;
        }

        [HttpGet("contacts/online")]
        public async Task<ActionResult<List<OnlineContact>>> GetOnlineContacts()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var contacts = await _mobilePhoneService.GetOnlineContactsAsync(userId.Value);
            return Ok(contacts);
        }

        [HttpGet("user/{phoneNumber}")]
        public async Task<ActionResult<User>> GetUserByPhoneNumber(string phoneNumber)
        {
            var user = await _mobilePhoneService.GetUserByPhoneNumberAsync(phoneNumber);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new { 
                id = user.Id, 
                displayName = user.DisplayName, 
                phoneNumber = user.PhoneNumber,
                profilePicture = user.ProfilePicture,
                isOnline = user.IsPhoneOnline 
            });
        }

        [HttpPost("call/initiate")]
        public async Task<ActionResult<PhoneCall>> InitiateCall([FromBody] InitiateCallRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var call = await _mobilePhoneService.InitiateCallAsync(userId.Value, request.PhoneNumber, request.IsVideoCall);
                return Ok(call);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("call/{callId}/answer")]
        public async Task<ActionResult<PhoneCall>> AnswerCall(int callId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var call = await _mobilePhoneService.AnswerCallAsync(callId, userId.Value);
            if (call == null)
                return NotFound(new { message = "Call not found or cannot be answered" });

            return Ok(call);
        }

        [HttpPost("call/{callId}/decline")]
        public async Task<ActionResult<PhoneCall>> DeclineCall(int callId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var call = await _mobilePhoneService.DeclineCallAsync(callId, userId.Value);
            if (call == null)
                return NotFound(new { message = "Call not found" });

            return Ok(call);
        }

        [HttpPost("call/{callId}/end")]
        public async Task<ActionResult<PhoneCall>> EndCall(int callId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var call = await _mobilePhoneService.EndCallAsync(callId, userId.Value);
            if (call == null)
                return NotFound(new { message = "Call not found" });

            return Ok(call);
        }

        [HttpGet("calls/history")]
        public async Task<ActionResult<List<PhoneCall>>> GetCallHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var calls = await _mobilePhoneService.GetCallHistoryAsync(userId.Value, page, pageSize);
            return Ok(calls);
        }

        [HttpPost("sms/send")]
        public async Task<ActionResult<SmsMessage>> SendSms([FromBody] SendSmsRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var sms = await _mobilePhoneService.SendSmsAsync(userId.Value, request.PhoneNumber, request.Content, request.Priority);
                return Ok(sms);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("sms/conversations")]
        public async Task<ActionResult<List<SmsConversation>>> GetSmsConversations()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var conversations = await _mobilePhoneService.GetSmsConversationsAsync(userId.Value);
            return Ok(conversations);
        }

        [HttpGet("sms/conversation/{phoneNumber}")]
        public async Task<ActionResult<List<SmsMessage>>> GetSmsConversation(string phoneNumber, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var messages = await _mobilePhoneService.GetSmsConversationAsync(userId.Value, phoneNumber, page, pageSize);
            return Ok(messages);
        }

        [HttpPost("sms/{messageId}/read")]
        public async Task<ActionResult> MarkSmsAsRead(int messageId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _mobilePhoneService.MarkSmsAsReadAsync(messageId, userId.Value);
            if (!success)
                return NotFound(new { message = "Message not found" });

            return Ok(new { message = "Message marked as read" });
        }

        [HttpPost("sms/conversation/{phoneNumber}/read")]
        public async Task<ActionResult> MarkConversationAsRead(string phoneNumber)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _mobilePhoneService.MarkConversationAsReadAsync(userId.Value, phoneNumber);
            return Ok(new { message = "Conversation marked as read" });
        }

        [HttpGet("sms/unread-count")]
        public async Task<ActionResult<int>> GetUnreadSmsCount()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var count = await _mobilePhoneService.GetUnreadSmsCountAsync(userId.Value);
            return Ok(count);
        }

        [HttpGet("calls/missed-count")]
        public async Task<ActionResult<int>> GetMissedCallsCount()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var count = await _mobilePhoneService.GetMissedCallsCountAsync(userId.Value);
            return Ok(count);
        }

        [HttpPost("contacts/add")]
        public async Task<ActionResult<PhoneContact>> AddContact([FromBody] AddContactRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var contact = await _mobilePhoneService.AddContactAsync(userId.Value, request.PhoneNumber, request.CustomName);
                return Ok(contact);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("contacts/{phoneNumber}")]
        public async Task<ActionResult> RemoveContact(string phoneNumber)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _mobilePhoneService.RemoveContactAsync(userId.Value, phoneNumber);
            if (!success)
                return NotFound(new { message = "Contact not found" });

            return Ok(new { message = "Contact removed" });
        }

        [HttpGet("contacts")]
        public async Task<ActionResult<List<PhoneContact>>> GetContacts([FromQuery] bool favoritesOnly = false)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var contacts = await _mobilePhoneService.GetContactsAsync(userId.Value, favoritesOnly);
            return Ok(contacts);
        }

        [HttpPost("contacts/{phoneNumber}/favorite")]
        public async Task<ActionResult> ToggleFavoriteContact(string phoneNumber)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _mobilePhoneService.ToggleFavoriteContactAsync(userId.Value, phoneNumber);
            return Ok(new { success });
        }

        [HttpPost("contacts/{phoneNumber}/block")]
        public async Task<ActionResult> ToggleBlockContact(string phoneNumber)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _mobilePhoneService.ToggleBlockContactAsync(userId.Value, phoneNumber);
            return Ok(new { success });
        }

        [HttpGet("notifications")]
        public async Task<ActionResult<List<PhoneNotification>>> GetNotifications()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var notifications = await _mobilePhoneService.GetUnreadNotificationsAsync(userId.Value);
            return Ok(notifications);
        }

        [HttpPost("notifications/{notificationId}/read")]
        public async Task<ActionResult> MarkNotificationAsRead(int notificationId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _mobilePhoneService.MarkNotificationAsReadAsync(notificationId, userId.Value);
            return Ok(new { success });
        }

        [HttpGet("settings")]
        public async Task<ActionResult<PhoneSettings>> GetPhoneSettings()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var settings = await _mobilePhoneService.GetPhoneSettingsAsync(userId.Value);
            return Ok(settings);
        }

        [HttpPut("settings")]
        public async Task<ActionResult<PhoneSettings>> UpdatePhoneSettings([FromBody] PhoneSettings settings)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var updatedSettings = await _mobilePhoneService.UpdatePhoneSettingsAsync(userId.Value, settings);
            return Ok(updatedSettings);
        }

        [HttpPost("status/online")]
        public async Task<ActionResult> SetOnlineStatus([FromBody] SetOnlineStatusRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            await _mobilePhoneService.UpdatePhoneOnlineStatusAsync(userId.Value, request.IsOnline);
            return Ok(new { message = "Status updated" });
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

    // Request DTOs
    public class InitiateCallRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsVideoCall { get; set; } = false;
    }

    public class SendSmsRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public SmsPriority Priority { get; set; } = SmsPriority.Normal;
    }

    public class AddContactRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string? CustomName { get; set; }
    }

    public class SetOnlineStatusRequest
    {
        public bool IsOnline { get; set; }
    }
}
