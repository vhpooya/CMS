using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class UserActivity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public ActivityType Type { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Action { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(100)]
        public string? Module { get; set; } // Calendar, Spreadsheet, Code, etc.
        
        public int? RelatedEntityId { get; set; } // ID of related entity (spreadsheet, event, etc.)
        
        [StringLength(100)]
        public string? RelatedEntityType { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [StringLength(50)]
        public string? IpAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        public int? SessionDurationMinutes { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? AdditionalData { get; set; } // JSON for extra data
        
        public ActivitySeverity Severity { get; set; } = ActivitySeverity.Info;
        
        public bool IsSuccessful { get; set; } = true;
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
    
    public class UserSession
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string SessionId { get; set; } = string.Empty;
        
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndTime { get; set; }
        
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
        
        [StringLength(50)]
        public string? IpAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        [StringLength(100)]
        public string? Browser { get; set; }
        
        [StringLength(100)]
        public string? OperatingSystem { get; set; }
        
        [StringLength(100)]
        public string? Device { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? LastActivityAt { get; set; }
        
        public int PageViews { get; set; } = 0;
        
        public int ActionsPerformed { get; set; } = 0;
        
        [StringLength(200)]
        public string? LastPage { get; set; }
        
        public SessionEndReason? EndReason { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
    
    public class UserPerformanceMetric
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        public int LoginCount { get; set; } = 0;
        
        public int TotalSessionMinutes { get; set; } = 0;
        
        public int SpreadsheetsCreated { get; set; } = 0;
        
        public int SpreadsheetsEdited { get; set; } = 0;
        
        public int EventsCreated { get; set; } = 0;
        
        public int CodeProjectsCreated { get; set; } = 0;
        
        public int CodeExecutions { get; set; } = 0;
        
        public int MessagesReceived { get; set; } = 0;
        
        public int MessagesSent { get; set; } = 0;
        
        public int FilesTransferred { get; set; } = 0;
        
        public int RemoteConnections { get; set; } = 0;
        
        public int ErrorsEncountered { get; set; } = 0;
        
        public double ProductivityScore { get; set; } = 0.0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
    
    public enum ActivityType
    {
        Login = 0,
        Logout = 1,
        Create = 2,
        Read = 3,
        Update = 4,
        Delete = 5,
        Execute = 6,
        Share = 7,
        Download = 8,
        Upload = 9,
        Export = 10,
        Import = 11,
        Print = 12,
        Search = 13,
        Navigate = 14,
        Error = 15,
        Security = 16,
        Admin = 17
    }
    
    public enum ActivitySeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3,
        Success = 4
    }
    
    public enum SessionEndReason
    {
        Logout = 0,
        Timeout = 1,
        Forced = 2,
        Error = 3,
        Browser = 4,
        Network = 5
    }
}
