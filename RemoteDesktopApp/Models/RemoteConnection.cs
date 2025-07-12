using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class RemoteConnection
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ControllerUserId { get; set; } // User who controls
        
        [Required]
        public int ControlledUserId { get; set; } // User being controlled
        
        [Required]
        [StringLength(100)]
        public string SessionId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ConnectionId { get; set; } = string.Empty; // SignalR connection ID
        
        public ConnectionStatus Status { get; set; } = ConnectionStatus.Connecting;
        
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndedAt { get; set; }
        
        public TimeSpan? Duration => EndedAt?.Subtract(StartedAt);
        
        public int ScreenQuality { get; set; } = 85;
        
        public int FrameRate { get; set; } = 30;
        
        public bool HasControl { get; set; } = true;
        
        public bool IsRecording { get; set; } = false;
        
        [StringLength(100)]
        public string? ControllerIpAddress { get; set; }
        
        [StringLength(500)]
        public string? DisconnectReason { get; set; }
        
        // Statistics
        public long BytesTransferred { get; set; } = 0;
        
        public int FramesSent { get; set; } = 0;
        
        public int InputEventsSent { get; set; } = 0;
        
        // Navigation properties
        [ForeignKey("ControllerUserId")]
        public virtual User ControllerUser { get; set; } = null!;
        
        [ForeignKey("ControlledUserId")]
        public virtual User ControlledUser { get; set; } = null!;
        
        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<FileTransfer> FileTransfers { get; set; } = new List<FileTransfer>();
    }
    
    public enum ConnectionStatus
    {
        Connecting = 0,
        Connected = 1,
        Disconnected = 2,
        Failed = 3,
        Terminated = 4
    }
}
