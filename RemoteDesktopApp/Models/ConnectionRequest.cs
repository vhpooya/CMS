using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class ConnectionRequest
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int RequesterId { get; set; }
        
        [Required]
        public int TargetUserId { get; set; }
        
        [Required]
        public ConnectionRequestStatus Status { get; set; } = ConnectionRequestStatus.Pending;
        
        [StringLength(500)]
        public string? Message { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? RespondedAt { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        [StringLength(500)]
        public string? ResponseMessage { get; set; }
        
        // Navigation properties
        [ForeignKey("RequesterId")]
        public virtual User Requester { get; set; } = null!;
        
        [ForeignKey("TargetUserId")]
        public virtual User TargetUser { get; set; } = null!;
    }
    
    public enum ConnectionRequestStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Expired = 3,
        Cancelled = 4
    }
}
