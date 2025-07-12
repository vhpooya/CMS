using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public interface IMobilePhoneService
    {
        /// <summary>
        /// Generates and assigns a unique 3-digit phone number to a user
        /// </summary>
        Task<string> AssignPhoneNumberAsync(int userId);
        
        /// <summary>
        /// Gets user by phone number
        /// </summary>
        Task<User?> GetUserByPhoneNumberAsync(string phoneNumber);
        
        /// <summary>
        /// Gets all online users with their phone numbers
        /// </summary>
        Task<List<OnlineContact>> GetOnlineContactsAsync(int currentUserId);
        
        /// <summary>
        /// Initiates a voice call between two users
        /// </summary>
        Task<PhoneCall> InitiateCallAsync(int callerId, string receiverPhoneNumber, bool isVideoCall = false);
        
        /// <summary>
        /// Answers an incoming call
        /// </summary>
        Task<PhoneCall?> AnswerCallAsync(int callId, int userId);
        
        /// <summary>
        /// Declines an incoming call
        /// </summary>
        Task<PhoneCall?> DeclineCallAsync(int callId, int userId, CallEndReason reason = CallEndReason.Declined);
        
        /// <summary>
        /// Ends an active call
        /// </summary>
        Task<PhoneCall?> EndCallAsync(int callId, int userId, CallEndReason reason = CallEndReason.Normal);
        
        /// <summary>
        /// Gets call history for a user
        /// </summary>
        Task<List<PhoneCall>> GetCallHistoryAsync(int userId, int page = 1, int pageSize = 50);
        
        /// <summary>
        /// Sends an SMS message
        /// </summary>
        Task<SmsMessage> SendSmsAsync(int senderId, string receiverPhoneNumber, string content, SmsPriority priority = SmsPriority.Normal);
        
        /// <summary>
        /// Gets SMS conversation between two users
        /// </summary>
        Task<List<SmsMessage>> GetSmsConversationAsync(int userId, string otherPhoneNumber, int page = 1, int pageSize = 50);
        
        /// <summary>
        /// Gets all SMS conversations for a user
        /// </summary>
        Task<List<SmsConversation>> GetSmsConversationsAsync(int userId);
        
        /// <summary>
        /// Marks SMS messages as read
        /// </summary>
        Task<bool> MarkSmsAsReadAsync(int messageId, int userId);
        
        /// <summary>
        /// Marks all SMS messages in a conversation as read
        /// </summary>
        Task<bool> MarkConversationAsReadAsync(int userId, string otherPhoneNumber);
        
        /// <summary>
        /// Gets unread SMS count for a user
        /// </summary>
        Task<int> GetUnreadSmsCountAsync(int userId);
        
        /// <summary>
        /// Adds a contact to user's phone book
        /// </summary>
        Task<PhoneContact> AddContactAsync(int userId, string phoneNumber, string? customName = null);
        
        /// <summary>
        /// Removes a contact from user's phone book
        /// </summary>
        Task<bool> RemoveContactAsync(int userId, string phoneNumber);
        
        /// <summary>
        /// Gets user's phone contacts
        /// </summary>
        Task<List<PhoneContact>> GetContactsAsync(int userId, bool favoritesOnly = false);
        
        /// <summary>
        /// Toggles favorite status for a contact
        /// </summary>
        Task<bool> ToggleFavoriteContactAsync(int userId, string phoneNumber);
        
        /// <summary>
        /// Blocks/unblocks a contact
        /// </summary>
        Task<bool> ToggleBlockContactAsync(int userId, string phoneNumber);
        
        /// <summary>
        /// Creates a phone notification
        /// </summary>
        Task<PhoneNotification> CreateNotificationAsync(int userId, NotificationType type, string title, string message, NotificationPriority priority = NotificationPriority.Normal);
        
        /// <summary>
        /// Gets unread notifications for a user
        /// </summary>
        Task<List<PhoneNotification>> GetUnreadNotificationsAsync(int userId);
        
        /// <summary>
        /// Marks notification as read
        /// </summary>
        Task<bool> MarkNotificationAsReadAsync(int notificationId, int userId);
        
        /// <summary>
        /// Gets or creates phone settings for a user
        /// </summary>
        Task<PhoneSettings> GetPhoneSettingsAsync(int userId);
        
        /// <summary>
        /// Updates phone settings for a user
        /// </summary>
        Task<PhoneSettings> UpdatePhoneSettingsAsync(int userId, PhoneSettings settings);
        
        /// <summary>
        /// Sets user's phone online status
        /// </summary>
        Task UpdatePhoneOnlineStatusAsync(int userId, bool isOnline);
        
        /// <summary>
        /// Gets missed calls count
        /// </summary>
        Task<int> GetMissedCallsCountAsync(int userId);
        
        /// <summary>
        /// Searches contacts by name or phone number
        /// </summary>
        Task<List<PhoneContact>> SearchContactsAsync(int userId, string query);
        
        /// <summary>
        /// Gets recent contacts based on call/SMS history
        /// </summary>
        Task<List<RecentContact>> GetRecentContactsAsync(int userId, int limit = 10);
        
        /// <summary>
        /// Checks if a phone number is available
        /// </summary>
        Task<bool> IsPhoneNumberAvailableAsync(string phoneNumber);
        
        /// <summary>
        /// Gets call statistics for a user
        /// </summary>
        Task<CallStatistics> GetCallStatisticsAsync(int userId);
        
        /// <summary>
        /// Gets SMS statistics for a user
        /// </summary>
        Task<SmsStatistics> GetSmsStatisticsAsync(int userId);
        
        /// <summary>
        /// Deletes old call history and SMS messages
        /// </summary>
        Task CleanupOldDataAsync(int daysToKeep = 90);
    }
    
    public class OnlineContact
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastActivity { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsBlocked { get; set; }
    }
    
    public class SmsConversation
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsBlocked { get; set; }
    }
    
    public class RecentContact
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public DateTime LastContactTime { get; set; }
        public string ContactType { get; set; } = string.Empty; // "call", "sms"
        public bool IsOnline { get; set; }
    }
    
    public class CallStatistics
    {
        public int TotalCalls { get; set; }
        public int IncomingCalls { get; set; }
        public int OutgoingCalls { get; set; }
        public int MissedCalls { get; set; }
        public TimeSpan TotalCallDuration { get; set; }
        public TimeSpan AverageCallDuration { get; set; }
        public int CallsToday { get; set; }
        public int CallsThisWeek { get; set; }
        public int CallsThisMonth { get; set; }
    }
    
    public class SmsStatistics
    {
        public int TotalMessages { get; set; }
        public int SentMessages { get; set; }
        public int ReceivedMessages { get; set; }
        public int UnreadMessages { get; set; }
        public int MessagesToday { get; set; }
        public int MessagesThisWeek { get; set; }
        public int MessagesThisMonth { get; set; }
        public int ActiveConversations { get; set; }
    }
}
