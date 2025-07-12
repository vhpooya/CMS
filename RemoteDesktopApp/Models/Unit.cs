using System.ComponentModel.DataAnnotations;

namespace RemoteDesktopApp.Models
{
    public class Unit
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string Code { get; set; } = string.Empty; // Unique unit code

        public int? ParentUnitId { get; set; }
        public Unit? ParentUnit { get; set; }

        public List<Unit> SubUnits { get; set; } = new();

        public List<User> Users { get; set; } = new();

        public List<UnitLink> LinkedUnits { get; set; } = new();
        public List<UnitLink> LinkedByUnits { get; set; } = new();

        public int? ManagerId { get; set; }
        public User? Manager { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneExtension { get; set; } = string.Empty;

        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        // Navigation properties for communication permissions
        public List<UnitCommunicationPermission> CommunicationPermissions { get; set; } = new();
        public List<UnitCommunicationPermission> AllowedCommunications { get; set; } = new();
    }

    public class UnitLink
    {
        public int Id { get; set; }

        public int SourceUnitId { get; set; }
        public Unit SourceUnit { get; set; } = null!;

        public int TargetUnitId { get; set; }
        public Unit TargetUnit { get; set; } = null!;

        public UnitLinkType LinkType { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class UnitCommunicationPermission
    {
        public int Id { get; set; }

        public int SourceUnitId { get; set; }
        public Unit SourceUnit { get; set; } = null!;

        public int TargetUnitId { get; set; }
        public Unit TargetUnit { get; set; } = null!;

        public CommunicationType CommunicationType { get; set; }
        public bool IsAllowed { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;
    }

    public enum UnitLinkType
    {
        Temporary = 1,      // موقت
        Permanent = 2,      // دائم
        Project = 3,        // پروژه‌ای
        Emergency = 4       // اضطراری
    }

    public enum CommunicationType
    {
        PhoneCall = 1,      // تماس تلفنی
        SMS = 2,            // پیامک
        Message = 3,        // پیام
        FileTransfer = 4,   // انتقال فایل
        RemoteDesktop = 5,  // دسکتاپ راه دور
        All = 6             // همه
    }
}
