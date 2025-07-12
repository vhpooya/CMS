using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class FileTransfer
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int SenderId { get; set; }
        
        [Required]
        public int ReceiverId { get; set; }
        
        public int? ConnectionId { get; set; } // Optional: link to remote connection
        
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        public long FileSize { get; set; }
        
        [StringLength(100)]
        public string? FileType { get; set; }
        
        [StringLength(64)]
        public string? FileHash { get; set; } // SHA256 hash for integrity
        
        public FileTransferStatus Status { get; set; } = FileTransferStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? StartedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        public long BytesTransferred { get; set; } = 0;
        
        public double ProgressPercentage => FileSize > 0 ? (double)BytesTransferred / FileSize * 100 : 0;
        
        public TimeSpan? TransferDuration => CompletedAt?.Subtract(StartedAt ?? CreatedAt);
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        // Transfer speed in bytes per second
        public double? TransferSpeed { get; set; }
        
        // Navigation properties
        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; } = null!;
        
        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; } = null!;
        
        [ForeignKey("ConnectionId")]
        public virtual RemoteConnection? Connection { get; set; }
    }
    
    public enum FileTransferStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4,
        Rejected = 5
    }
}
