using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(12)]
        public string ClientId { get; set; } = string.Empty; // Unique 12-character ID
        
        public bool IsActive { get; set; } = true;
        
        public bool IsOnline { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
        
        public DateTime? LastSeenAt { get; set; }
        
        [StringLength(200)]
        public string? ProfilePicture { get; set; }
        
        [StringLength(500)]
        public string? Bio { get; set; }

            
        public UserRole Role { get; set; } = UserRole.User;

        public int? CreatedByUserId { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        [StringLength(3)]
        public string? PhoneNumber { get; set; } // 3-digit unique phone number

        public bool IsPhoneOnline { get; set; } = false;

        public bool PhoneNotificationsEnabled { get; set; } = true;

        public bool PhoneSoundEnabled { get; set; } = true;

        public DateTime? LastPhoneActivity { get; set; }

        public DateTime? LastActivityAt { get; set; }

        public bool IsDeactivated { get; set; } = false;

        public DateTime? DeactivatedAt { get; set; }

        public int? DeactivatedByUserId { get; set; }

        [StringLength(500)]
        public string? DeactivationReason { get; set; }

        // Unit relationship
        public int? UnitId { get; set; }
        public virtual Unit? Unit { get; set; }

        // Navigation properties
        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("DeactivatedByUserId")]
        public virtual User? DeactivatedBy { get; set; }
        public virtual ICollection<RemoteConnection> OutgoingConnections { get; set; } = new List<RemoteConnection>();
        public virtual ICollection<RemoteConnection> IncomingConnections { get; set; } = new List<RemoteConnection>();
        public virtual ICollection<ConnectionRequest> SentRequests { get; set; } = new List<ConnectionRequest>();
        public virtual ICollection<ConnectionRequest> ReceivedRequests { get; set; } = new List<ConnectionRequest>();
        public virtual ICollection<ChatMessage> SentMessages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<ChatMessage> ReceivedMessages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<FileTransfer> SentFiles { get; set; } = new List<FileTransfer>();
        public virtual ICollection<FileTransfer> ReceivedFiles { get; set; } = new List<FileTransfer>();
        public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
        public virtual ICollection<Spreadsheet> Spreadsheets { get; set; } = new List<Spreadsheet>();
        public virtual ICollection<CodeProject> CodeProjects { get; set; } = new List<CodeProject>();
        public virtual ICollection<UserActivity> Activities { get; set; } = new List<UserActivity>();
        public virtual ICollection<Conversation> CreatedConversations { get; set; } = new List<Conversation>();
        public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();
        public virtual ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
        public virtual ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();
        public virtual ICollection<UserGroupMember> GroupMemberships { get; set; } = new List<UserGroupMember>();
        public virtual ICollection<UserGroup> CreatedGroups { get; set; } = new List<UserGroup>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        public virtual ICollection<CodeLibrary> CodeLibraries { get; set; } = new List<CodeLibrary>();
        public virtual ICollection<CodeLibraryRating> CodeLibraryRatings { get; set; } = new List<CodeLibraryRating>();
        public virtual ICollection<PhoneCall> OutgoingCalls { get; set; } = new List<PhoneCall>();
        public virtual ICollection<PhoneCall> IncomingCalls { get; set; } = new List<PhoneCall>();
        public virtual ICollection<SmsMessage> SentSmsMessages { get; set; } = new List<SmsMessage>();
        public virtual ICollection<SmsMessage> ReceivedSmsMessages { get; set; } = new List<SmsMessage>();
        public virtual ICollection<PhoneContact> PhoneContacts { get; set; } = new List<PhoneContact>();
        public virtual ICollection<PhoneContact> ContactedBy { get; set; } = new List<PhoneContact>();
        public virtual ICollection<PhoneNotification> PhoneNotifications { get; set; } = new List<PhoneNotification>();
        public virtual PhoneSettings? PhoneSettings { get; set; }
    }

    public enum UserRole
    {
        User = 0,
        Admin = 1
    }
}
