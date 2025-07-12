using RemoteDesktopApp.Models;
using System.ComponentModel.DataAnnotations;

namespace RemoteDesktopApp.ViewModels
{
    public class RegisterViewModels
    {
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
        public UserRole Role { get; set; } = UserRole.User;

        [Required]
        [StringLength(12)]
        public string ClientId { get; set; } = string.Empty; // Unique 12-character ID

        public bool IsActive { get; set; } = true;

        public bool IsOnline { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? UnitId { get; set; }
        public virtual Unit? Unit { get; set; }

        [StringLength(3)]
        public string? PhoneNumber { get; set; } // 3-digit unique phone number

        public bool IsPhoneOnline { get; set; } = false;

        public bool PhoneNotificationsEnabled { get; set; } = true;

        public bool PhoneSoundEnabled { get; set; } = true;

     

    
    }
}
