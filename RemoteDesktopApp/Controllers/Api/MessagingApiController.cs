using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteDesktopApp.Models;
using RemoteDesktopApp.Services;

namespace RemoteDesktopApp.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MessagingApiController : ControllerBase
    {
        private readonly IMessagingService _messagingService;
        private readonly ILogger<MessagingApiController> _logger;

        public MessagingApiController(IMessagingService messagingService, ILogger<MessagingApiController> logger)
        {
            _messagingService = messagingService;
            _logger = logger;
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<List<ConversationSummary>>> GetConversations()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var conversations = await _messagingService.GetUserConversationsAsync(userId.Value);
            return Ok(conversations);
        }

        [HttpGet("conversations/{id}")]
        public async Task<ActionResult<Conversation>> GetConversation(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var conversation = await _messagingService.GetConversationByIdAsync(id, userId.Value);
                if (conversation == null)
                    return NotFound();

                return Ok(conversation);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("conversations/{id}/messages")]
        public async Task<ActionResult<List<ChatMessage>>> GetConversationMessages(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var messages = await _messagingService.GetConversationMessagesAsync(id, userId.Value, page, pageSize);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("direct/{userId}/messages")]
        public async Task<ActionResult<List<ChatMessage>>> GetDirectMessages(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var messages = await _messagingService.GetDirectMessagesAsync(currentUserId.Value, userId, page, pageSize);
            return Ok(messages);
        }

        [HttpPost("direct")]
        public async Task<ActionResult<ChatMessage>> SendDirectMessage([FromBody] SendDirectMessageRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var message = await _messagingService.SendDirectMessageAsync(
                    userId.Value, 
                    request.ReceiverId, 
                    request.Content, 
                    request.Type,
                    request.AttachmentUrl);

                return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending direct message from user {UserId} to {ReceiverId}", userId, request.ReceiverId);
                return StatusCode(500, new { message = "An error occurred while sending the message" });
            }
        }

        [HttpPost("conversations/{id}/messages")]
        public async Task<ActionResult<ChatMessage>> SendConversationMessage(int id, [FromBody] SendConversationMessageRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var message = await _messagingService.SendConversationMessageAsync(
                    userId.Value, 
                    id, 
                    request.Content, 
                    request.Type,
                    request.AttachmentUrl);

                return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to conversation {ConversationId} by user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while sending the message" });
            }
        }

        [HttpPost("conversations")]
        public async Task<ActionResult<Conversation>> CreateGroupConversation([FromBody] CreateGroupConversationRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var conversation = await _messagingService.CreateGroupConversationAsync(
                    userId.Value, 
                    request.Name, 
                    request.Description, 
                    request.ParticipantIds);

                return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group conversation by user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while creating the conversation" });
            }
        }

        [HttpGet("messages/{id}")]
        public async Task<ActionResult<ChatMessage>> GetMessage(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var message = await _messagingService.GetMessageByIdAsync(id, userId.Value);
            if (message == null)
                return NotFound();

            return Ok(message);
        }

        [HttpPut("messages/{id}")]
        public async Task<ActionResult<ChatMessage>> EditMessage(int id, [FromBody] EditMessageRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var message = await _messagingService.EditMessageAsync(id, userId.Value, request.Content);
            if (message == null)
                return NotFound();

            return Ok(message);
        }

        [HttpDelete("messages/{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _messagingService.DeleteMessageAsync(id, userId.Value);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPost("messages/{id}/read")]
        public async Task<ActionResult> MarkMessageAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _messagingService.MarkMessageAsReadAsync(id, userId.Value);
            if (!success)
                return NotFound();

            return Ok(new { message = "Message marked as read" });
        }

        [HttpPost("conversations/{id}/read")]
        public async Task<ActionResult> MarkConversationAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _messagingService.MarkConversationAsReadAsync(id, userId.Value);
            if (!success)
                return NotFound();

            return Ok(new { message = "Conversation marked as read" });
        }

        [HttpPost("messages/{id}/reactions")]
        public async Task<ActionResult<MessageReaction>> AddReaction(int id, [FromBody] AddReactionRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var reaction = await _messagingService.AddReactionAsync(id, userId.Value, request.Emoji);
                return Ok(reaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reaction to message {MessageId} by user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while adding the reaction" });
            }
        }

        [HttpDelete("messages/{id}/reactions/{emoji}")]
        public async Task<ActionResult> RemoveReaction(int id, string emoji)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var success = await _messagingService.RemoveReactionAsync(id, userId.Value, emoji);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<ChatMessage>>> SearchMessages(
            [FromQuery] string query, 
            [FromQuery] int? conversationId = null, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Search query is required" });

            var messages = await _messagingService.SearchMessagesAsync(userId.Value, query, conversationId, page, pageSize);
            return Ok(messages);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var count = await _messagingService.GetUnreadMessageCountAsync(userId.Value);
            return Ok(count);
        }

        [HttpGet("online-users")]
        public async Task<ActionResult<List<User>>> GetOnlineUsers()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var users = await _messagingService.GetOnlineUsersAsync(userId.Value);
            return Ok(users);
        }

        [HttpGet("recent-contacts")]
        public async Task<ActionResult<List<User>>> GetRecentContacts([FromQuery] int limit = 10)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var contacts = await _messagingService.GetRecentContactsAsync(userId.Value, limit);
            return Ok(contacts);
        }

        [HttpPost("upload")]
        public async Task<ActionResult<string>> UploadAttachment([FromForm] IFormFile file)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            try
            {
                var url = await _messagingService.UploadAttachmentAsync(file, userId.Value);
                return Ok(new { url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while uploading the file" });
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

    // Request DTOs
    public class SendDirectMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Text;
        public string? AttachmentUrl { get; set; }
    }

    public class SendConversationMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Text;
        public string? AttachmentUrl { get; set; }
    }

    public class CreateGroupConversationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<int> ParticipantIds { get; set; } = new();
    }

    public class EditMessageRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class AddReactionRequest
    {
        public string Emoji { get; set; } = string.Empty;
    }
}
