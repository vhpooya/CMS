using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteDesktopApp.Models
{
    public class UserGroup
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public int CreatedByUserId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsSystemGroup { get; set; } = false; // For built-in groups like Admins, Users
        
        [StringLength(50)]
        public string? Color { get; set; } // For UI display
        
        [StringLength(50)]
        public string? Icon { get; set; } // For UI display
        
        // Navigation properties
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;
        
        public virtual ICollection<UserGroupMember> Members { get; set; } = new List<UserGroupMember>();
        public virtual ICollection<GroupPermission> Permissions { get; set; } = new List<GroupPermission>();
    }
    
    public class UserGroupMember
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int GroupId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int AddedByUserId { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;
        
        // Navigation properties
        [ForeignKey("GroupId")]
        public virtual UserGroup Group { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        [ForeignKey("AddedByUserId")]
        public virtual User AddedBy { get; set; } = null!;
    }
    
    public class Permission
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty; // Unique identifier like "calendar.view", "messages.send"
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Module { get; set; } = string.Empty; // calendar, messages, spreadsheets, etc.
        
        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // view, create, edit, delete, admin
        
        public PermissionLevel Level { get; set; } = PermissionLevel.Basic;
        
        public bool IsSystemPermission { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<GroupPermission> GroupPermissions { get; set; } = new List<GroupPermission>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
    
    public class GroupPermission
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int GroupId { get; set; }
        
        [Required]
        public int PermissionId { get; set; }
        
        [Required]
        public int GrantedByUserId { get; set; }
        
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        [ForeignKey("GroupId")]
        public virtual UserGroup Group { get; set; } = null!;
        
        [ForeignKey("PermissionId")]
        public virtual Permission Permission { get; set; } = null!;
        
        [ForeignKey("GrantedByUserId")]
        public virtual User GrantedBy { get; set; } = null!;
    }
    
    public class UserPermission
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int PermissionId { get; set; }
        
        [Required]
        public int GrantedByUserId { get; set; }
        
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        public PermissionSource Source { get; set; } = PermissionSource.Direct;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        [ForeignKey("PermissionId")]
        public virtual Permission Permission { get; set; } = null!;
        
        [ForeignKey("GrantedByUserId")]
        public virtual User GrantedBy { get; set; } = null!;
    }
    
    public class CodeLibrary
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Language { get; set; } = "javascript";
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Tags { get; set; } // Comma-separated tags
        
        [Required]
        public int CreatedByUserId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsPublic { get; set; } = false;
        
        public bool IsTemplate { get; set; } = false;
        
        public int UsageCount { get; set; } = 0;
        
        public double Rating { get; set; } = 0.0;
        
        public int RatingCount { get; set; } = 0;
        
        // Navigation properties
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; } = null!;
        
        public virtual ICollection<CodeLibraryRating> Ratings { get; set; } = new List<CodeLibraryRating>();
    }
    
    public class CodeLibraryRating
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int CodeLibraryId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Range(1, 5)]
        public int Rating { get; set; }
        
        [StringLength(500)]
        public string? Comment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("CodeLibraryId")]
        public virtual CodeLibrary CodeLibrary { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    public class CodeExecutionHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        public string? Output { get; set; }

        public string? Error { get; set; }

        public bool Success { get; set; }

        public TimeSpan ExecutionTime { get; set; }

        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    public enum GroupMemberRole
    {
        Member = 0,
        Moderator = 1,
        Admin = 2
    }
    
    public enum PermissionLevel
    {
        Basic = 0,
        Intermediate = 1,
        Advanced = 2,
        Admin = 3
    }
    
    public enum PermissionSource
    {
        Direct = 0,
        Group = 1,
        Role = 2,
        System = 3
    }
}
