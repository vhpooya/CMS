using System.ComponentModel.DataAnnotations;

namespace RemoteDesktopApp.Models
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class RegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string DisplayName { get; set; } = string.Empty;
    }
    
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserInfo? User { get; set; }
    }
    
    public class UserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }
    
    public class RemoteDesktopSettings
    {
        public int MaxConnections { get; set; } = 10;
        public int ScreenCaptureQuality { get; set; } = 85;
        public int FrameRate { get; set; } = 30;
        public bool EnableFileTransfer { get; set; } = true;
        public bool EnableClipboardSync { get; set; } = true;
        public bool RequireAuthentication { get; set; } = true;
        public List<string> AllowedUsers { get; set; } = new();
    }
}
