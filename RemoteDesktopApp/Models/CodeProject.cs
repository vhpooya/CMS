using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class CodeProject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public CodeLanguage Language { get; set; } = CodeLanguage.JavaScript;
        
        [Column(TypeName = "nvarchar(max)")]
        public string? Code { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? HtmlTemplate { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? CssStyles { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? LastRunAt { get; set; }
        
        public bool IsPublic { get; set; } = false;
        
        public bool IsTemplate { get; set; } = false;
        
        [StringLength(100)]
        public string? Category { get; set; }
        
        [StringLength(200)]
        public string? Tags { get; set; }
        
        public int RunCount { get; set; } = 0;
        
        public bool HasLicense { get; set; } = false;
        
        [StringLength(100)]
        public string? LicenseType { get; set; }
        
        public DateTime? LicenseExpiresAt { get; set; }
        
        [StringLength(500)]
        public string? LicenseKey { get; set; }
        
        public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
        
        public int Version { get; set; } = 1;
        
        public long CodeSize { get; set; } = 0;
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        public virtual ICollection<CodeExecution> Executions { get; set; } = new List<CodeExecution>();
        public virtual ICollection<CodeVersion> Versions { get; set; } = new List<CodeVersion>();
    }
    
    public class CodeExecution
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProjectId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        
        public ExecutionStatus Status { get; set; } = ExecutionStatus.Running;
        
        public int ExecutionTimeMs { get; set; } = 0;
        
        [Column(TypeName = "nvarchar(max)")]
        public string? Output { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? ErrorOutput { get; set; }
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        public int? ExitCode { get; set; }
        
        [StringLength(100)]
        public string? UserAgent { get; set; }
        
        [StringLength(50)]
        public string? IpAddress { get; set; }
        
        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual CodeProject Project { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
    
    public class CodeVersion
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProjectId { get; set; }
        
        [Required]
        public int Version { get; set; }
        
        [Required]
        public int CreatedByUserId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(500)]
        public string? ChangeDescription { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? Code { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? HtmlTemplate { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? CssStyles { get; set; }
        
        public long CodeSize { get; set; } = 0;
        
        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual CodeProject Project { get; set; } = null!;
        
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;
    }
    
    public enum CodeLanguage
    {
        JavaScript = 0,
        TypeScript = 1,
        HTML = 2,
        CSS = 3,
        Python = 4,
        CSharp = 5,
        Java = 6,
        SQL = 7
    }
    
    public enum ProjectStatus
    {
        Draft = 0,
        Active = 1,
        Completed = 2,
        Archived = 3,
        Deleted = 4
    }
    
    public enum ExecutionStatus
    {
        Running = 0,
        Completed = 1,
        Failed = 2,
        Timeout = 3,
        Cancelled = 4
    }
}
