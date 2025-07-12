using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        public int? ReceiverId { get; set; } // Nullable for group messages

        public int? ConversationId { get; set; } // For group conversations

        public int? ConnectionId { get; set; } // Optional: link to remote connection

        [Required]
        [StringLength(5000)]
        public string Content { get; set; } = string.Empty;

        public MessageType Type { get; set; } = MessageType.Text;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public bool IsEdited { get; set; } = false;

        public DateTime? EditedAt { get; set; }

        public int? ReplyToMessageId { get; set; }

        [StringLength(500)]
        public string? AttachmentUrl { get; set; }

        [StringLength(100)]
        public string? AttachmentName { get; set; }

        public long? AttachmentSize { get; set; }

        [StringLength(50)]
        public string? AttachmentType { get; set; }

        public MessageStatus Status { get; set; } = MessageStatus.Sent;

        public MessagePriority Priority { get; set; } = MessagePriority.Normal;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; } = null!;

        [ForeignKey("ReceiverId")]
        public virtual User? Receiver { get; set; }

        [ForeignKey("ConversationId")]
        public virtual Conversation? Conversation { get; set; }

        [ForeignKey("ConnectionId")]
        public virtual RemoteConnection? Connection { get; set; }

        [ForeignKey("ReplyToMessageId")]
        public virtual ChatMessage? ReplyToMessage { get; set; }

        public virtual ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public virtual ICollection<MessageRead> ReadReceipts { get; set; } = new List<MessageRead>();
    }

    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public ConversationType Type { get; set; } = ConversationType.Direct;

        [Required]
        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastMessageAt { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        // Navigation properties
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;

        public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ConversationParticipant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        public int UserId { get; set; }

        public ParticipantRole Role { get; set; } = ParticipantRole.Member;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastSeenAt { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsMuted { get; set; } = false;

        // Navigation properties
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    public class MessageReaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(10)]
        public string Emoji { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("MessageId")]
        public virtual ChatMessage Message { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    public class MessageRead
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("MessageId")]
        public virtual ChatMessage Message { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    public enum MessageType
    {
        Text = 0,
        Image = 1,
        File = 2,
        Audio = 3,
        Video = 4,
        Link = 5,
        System = 6
    }

    public enum MessageStatus
    {
        Sent = 0,
        Delivered = 1,
        Read = 2,
        Failed = 3
    }

    public enum MessagePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }

    public enum ConversationType
    {
        Direct = 0,
        Group = 1,
        Channel = 2
    }

    public enum ParticipantRole
    {
        Member = 0,
        Admin = 1,
        Owner = 2
    }
}
