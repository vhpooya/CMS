using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public interface IMessagingService
    {
        /// <summary>
        /// Sends a direct message to another user
        /// </summary>
        Task<ChatMessage> SendDirectMessageAsync(int senderId, int receiverId, string content, MessageType type = MessageType.Text, string? attachmentUrl = null);
        
        /// <summary>
        /// Sends a message to a conversation/group
        /// </summary>
        Task<ChatMessage> SendConversationMessageAsync(int senderId, int conversationId, string content, MessageType type = MessageType.Text, string? attachmentUrl = null);
        
        /// <summary>
        /// Gets direct messages between two users
        /// </summary>
        Task<List<ChatMessage>> GetDirectMessagesAsync(int userId1, int userId2, int page = 1, int pageSize = 50);
        
        /// <summary>
        /// Gets messages from a conversation
        /// </summary>
        Task<List<ChatMessage>> GetConversationMessagesAsync(int conversationId, int userId, int page = 1, int pageSize = 50);
        
        /// <summary>
        /// Gets all conversations for a user
        /// </summary>
        Task<List<ConversationSummary>> GetUserConversationsAsync(int userId);
        
        /// <summary>
        /// Creates a new group conversation
        /// </summary>
        Task<Conversation> CreateGroupConversationAsync(int creatorId, string name, string? description, List<int> participantIds);
        
        /// <summary>
        /// Adds a user to a conversation
        /// </summary>
        Task<ConversationParticipant> AddParticipantAsync(int conversationId, int userId, int addedByUserId, ParticipantRole role = ParticipantRole.Member);
        
        /// <summary>
        /// Removes a user from a conversation
        /// </summary>
        Task<bool> RemoveParticipantAsync(int conversationId, int userId, int removedByUserId);
        
        /// <summary>
        /// Updates participant role
        /// </summary>
        Task<bool> UpdateParticipantRoleAsync(int conversationId, int userId, int updatedByUserId, ParticipantRole newRole);
        
        /// <summary>
        /// Marks a message as read
        /// </summary>
        Task<bool> MarkMessageAsReadAsync(int messageId, int userId);
        
        /// <summary>
        /// Marks all messages in a conversation as read
        /// </summary>
        Task<bool> MarkConversationAsReadAsync(int conversationId, int userId);
        
        /// <summary>
        /// Gets unread message count for a user
        /// </summary>
        Task<int> GetUnreadMessageCountAsync(int userId);
        
        /// <summary>
        /// Gets unread message count for a specific conversation
        /// </summary>
        Task<int> GetUnreadConversationCountAsync(int conversationId, int userId);
        
        /// <summary>
        /// Edits a message
        /// </summary>
        Task<ChatMessage?> EditMessageAsync(int messageId, int userId, string newContent);
        
        /// <summary>
        /// Deletes a message
        /// </summary>
        Task<bool> DeleteMessageAsync(int messageId, int userId);
        
        /// <summary>
        /// Adds a reaction to a message
        /// </summary>
        Task<MessageReaction> AddReactionAsync(int messageId, int userId, string emoji);
        
        /// <summary>
        /// Removes a reaction from a message
        /// </summary>
        Task<bool> RemoveReactionAsync(int messageId, int userId, string emoji);
        
        /// <summary>
        /// Searches messages
        /// </summary>
        Task<List<ChatMessage>> SearchMessagesAsync(int userId, string query, int? conversationId = null, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Gets message by ID
        /// </summary>
        Task<ChatMessage?> GetMessageByIdAsync(int messageId, int userId);
        
        /// <summary>
        /// Gets conversation by ID
        /// </summary>
        Task<Conversation?> GetConversationByIdAsync(int conversationId, int userId);
        
        /// <summary>
        /// Updates conversation details
        /// </summary>
        Task<Conversation?> UpdateConversationAsync(int conversationId, int userId, string? name = null, string? description = null);
        
        /// <summary>
        /// Gets online users for messaging
        /// </summary>
        Task<List<User>> GetOnlineUsersAsync(int currentUserId);
        
        /// <summary>
        /// Gets recent contacts for a user
        /// </summary>
        Task<List<User>> GetRecentContactsAsync(int userId, int limit = 10);
        
        /// <summary>
        /// Uploads an attachment for messaging
        /// </summary>
        Task<string> UploadAttachmentAsync(IFormFile file, int userId);
        
        /// <summary>
        /// Gets conversation participants
        /// </summary>
        Task<List<ConversationParticipant>> GetConversationParticipantsAsync(int conversationId, int userId);
        
        /// <summary>
        /// Updates user's last seen time in conversation
        /// </summary>
        Task UpdateLastSeenAsync(int conversationId, int userId);
        
        /// <summary>
        /// Mutes/unmutes a conversation for a user
        /// </summary>
        Task<bool> ToggleConversationMuteAsync(int conversationId, int userId);
    }
    
    public class ConversationSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ConversationType Type { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public string? LastMessageContent { get; set; }
        public string? LastMessageSender { get; set; }
        public int UnreadCount { get; set; }
        public bool IsMuted { get; set; }
        public List<ConversationParticipant> Participants { get; set; } = new();
        public bool IsOnline { get; set; } // For direct conversations
    }
}
