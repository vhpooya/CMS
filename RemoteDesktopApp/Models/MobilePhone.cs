using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class PhoneCall
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int CallerId { get; set; }
        
        [Required]
        public int ReceiverId { get; set; }
        
        [Required]
        [StringLength(3)]
        public string CallerPhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(3)]
        public string ReceiverPhoneNumber { get; set; } = string.Empty;
        
        public CallStatus Status { get; set; } = CallStatus.Initiated;
        
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndTime { get; set; }
        
        public TimeSpan? Duration { get; set; }
        
        public CallEndReason? EndReason { get; set; }
        
        [StringLength(100)]
        public string? ConnectionId { get; set; } // SignalR connection ID
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public bool IsVideoCall { get; set; } = false;
        
        public CallQuality? Quality { get; set; }
        
        // Navigation properties
        [ForeignKey("CallerId")]
        public virtual User Caller { get; set; } = null!;
        
        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; } = null!;
    }
    
    public class SmsMessage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int SenderId { get; set; }
        
        [Required]
        public int ReceiverId { get; set; }
        
        [Required]
        [StringLength(3)]
        public string SenderPhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(3)]
        public string ReceiverPhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;
        
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? DeliveredAt { get; set; }
        
        public DateTime? ReadAt { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public bool IsDelivered { get; set; } = false;
        
        public SmsStatus Status { get; set; } = SmsStatus.Sent;
        
        public SmsPriority Priority { get; set; } = SmsPriority.Normal;
        
        [StringLength(500)]
        public string? AttachmentUrl { get; set; }
        
        [StringLength(100)]
        public string? AttachmentName { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        public DateTime? DeletedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; } = null!;
        
        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; } = null!;
    }
    
    public class PhoneContact
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int ContactUserId { get; set; }
        
        [StringLength(100)]
        public string? ContactName { get; set; } // Custom name for the contact
        
        [StringLength(3)]
        public string ContactPhoneNumber { get; set; } = string.Empty;
        
        public bool IsFavorite { get; set; } = false;
        
        public bool IsBlocked { get; set; } = false;
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastContactAt { get; set; }
        
        public int CallCount { get; set; } = 0;
        
        public int MessageCount { get; set; } = 0;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        [ForeignKey("ContactUserId")]
        public virtual User ContactUser { get; set; } = null!;
    }
    
    public class PhoneNotification
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        public NotificationType Type { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsRead { get; set; } = false;
        
        public DateTime? ReadAt { get; set; }
        
        public bool IsDisplayed { get; set; } = false;
        
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        
        [StringLength(100)]
        public string? ActionUrl { get; set; }
        
        [StringLength(50)]
        public string? Icon { get; set; }
        
        [StringLength(20)]
        public string? Color { get; set; }
        
        public int? RelatedId { get; set; } // Related call or message ID
        
        [StringLength(50)]
        public string? RelatedType { get; set; } // "call", "sms", etc.
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
    
    public class PhoneSettings
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        public bool NotificationsEnabled { get; set; } = true;
        
        public bool SoundEnabled { get; set; } = true;
        
        public bool VibrationEnabled { get; set; } = true;
        
        public bool ShowOnlineStatus { get; set; } = true;
        
        public bool AutoAnswerEnabled { get; set; } = false;
        
        public int AutoAnswerDelay { get; set; } = 10; // seconds
        
        [StringLength(100)]
        public string RingtoneUrl { get; set; } = "/sounds/default-ringtone.mp3";
        
        [StringLength(100)]
        public string NotificationSoundUrl { get; set; } = "/sounds/notification.mp3";
        
        public int RingtoneVolume { get; set; } = 80; // 0-100
        
        public int NotificationVolume { get; set; } = 60; // 0-100
        
        public bool DoNotDisturbEnabled { get; set; } = false;
        
        public TimeSpan? DoNotDisturbStart { get; set; }
        
        public TimeSpan? DoNotDisturbEnd { get; set; }
        
        public bool CallForwardingEnabled { get; set; } = false;
        
        [StringLength(3)]
        public string? ForwardToPhoneNumber { get; set; }
        
        public bool ShowTypingIndicator { get; set; } = true;
        
        public bool ReadReceiptsEnabled { get; set; } = true;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
    
    public enum CallStatus
    {
        Initiated = 0,
        Ringing = 1,
        Answered = 2,
        InProgress = 3,
        Ended = 4,
        Missed = 5,
        Declined = 6,
        Busy = 7,
        Failed = 8
    }
    
    public enum CallEndReason
    {
        Normal = 0,
        Declined = 1,
        Missed = 2,
        Busy = 3,
        NetworkError = 4,
        UserHangup = 5,
        Timeout = 6
    }
    
    public enum CallQuality
    {
        Poor = 1,
        Fair = 2,
        Good = 3,
        Excellent = 4
    }
    
    public enum SmsStatus
    {
        Sent = 0,
        Delivered = 1,
        Read = 2,
        Failed = 3
    }
    
    public enum SmsPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }
    
    public enum NotificationType
    {
        IncomingCall = 0,
        MissedCall = 1,
        NewSms = 2,
        CallEnded = 3,
        SystemNotification = 4,
        ContactOnline = 5,
        ContactOffline = 6
    }
    
    public enum NotificationPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }
}
