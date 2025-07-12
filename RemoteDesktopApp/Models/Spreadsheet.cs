using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class Spreadsheet
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int OwnerId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? LastAccessedAt { get; set; }
        
        public bool IsPublic { get; set; } = false;
        
        public bool IsTemplate { get; set; } = false;
        
        [StringLength(100)]
        public string? Category { get; set; }
        
        [StringLength(50)]
        public string? Tags { get; set; }
        
        public int RowCount { get; set; } = 100;
        
        public int ColumnCount { get; set; } = 26; // A-Z
        
        // JSON serialized spreadsheet data
        [Column(TypeName = "nvarchar(max)")]
        public string? Data { get; set; }
        
        // JSON serialized formatting data
        [Column(TypeName = "nvarchar(max)")]
        public string? Formatting { get; set; }
        
        // JSON serialized chart configurations
        [Column(TypeName = "nvarchar(max)")]
        public string? Charts { get; set; }
        
        public long FileSize { get; set; } = 0;
        
        public int Version { get; set; } = 1;
        
        // Navigation properties
        [ForeignKey("OwnerId")]
        public virtual User Owner { get; set; } = null!;
        
        public virtual ICollection<SpreadsheetShare> Shares { get; set; } = new List<SpreadsheetShare>();
        public virtual ICollection<SpreadsheetVersion> Versions { get; set; } = new List<SpreadsheetVersion>();
    }
    
    public class SpreadsheetShare
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int SpreadsheetId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public SharePermission Permission { get; set; } = SharePermission.View;
        
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;
        
        public int SharedByUserId { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        [ForeignKey("SpreadsheetId")]
        public virtual Spreadsheet Spreadsheet { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        [ForeignKey("SharedByUserId")]
        public virtual User SharedBy { get; set; } = null!;
    }
    
    public class SpreadsheetVersion
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int SpreadsheetId { get; set; }
        
        [Required]
        public int Version { get; set; }
        
        [Required]
        public int CreatedByUserId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(500)]
        public string? ChangeDescription { get; set; }
        
        // JSON serialized spreadsheet data for this version
        [Column(TypeName = "nvarchar(max)")]
        public string? Data { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? Formatting { get; set; }
        
        public long FileSize { get; set; } = 0;
        
        // Navigation properties
        [ForeignKey("SpreadsheetId")]
        public virtual Spreadsheet Spreadsheet { get; set; } = null!;
        
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;
    }
    
    public enum SharePermission
    {
        View = 0,
        Comment = 1,
        Edit = 2,
        Owner = 3
    }
}
