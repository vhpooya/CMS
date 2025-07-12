using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public interface IUserService
    {

        /// <summary>
        /// لیست کلیه کاربران
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<User>> GetAllUsersAsync();

        /// <summary>
        /// لیست کلیه کاربران فعال
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<User>> GetAllActiveUsersAsync();

        /// <summary>
        /// Authenticates a user with username and password
        /// </summary>
        Task<User?> AuthenticateAsync(string username, string password);
        
        /// <summary>
        /// Creates a new user account
        /// </summary>
        Task<User> CreateUserAsync(string username, string email, string password, string displayName, UserRole role = UserRole.User, int? createdByUserId = null);
        
        /// <summary>
        /// Gets a user by their unique client ID
        /// </summary>
        Task<User?> GetUserByClientIdAsync(string clientId);
        
        /// <summary>
        /// Gets a user by their username
        /// </summary>
        Task<User?> GetUserByUsernameAsync(string username);
        
        /// <summary>
        /// Gets a user by their ID
        /// </summary>
        Task<User?> GetUserByIdAsync(int userId);
        
        /// <summary>
        /// Updates user's online status
        /// </summary>
        Task UpdateUserOnlineStatusAsync(int userId, bool isOnline);
        
        /// <summary>
        /// Updates user's last seen timestamp
        /// </summary>
        Task UpdateLastSeenAsync(int userId);
        
        /// <summary>
        /// Generates a unique 12-character client ID
        /// </summary>
        Task<string> GenerateUniqueClientIdAsync();
        
        /// <summary>
        /// Searches for users by display name or username
        /// </summary>
        Task<List<User>> SearchUsersAsync(string searchTerm, int currentUserId);
        
        /// <summary>
        /// Gets all online users
        /// </summary>
        Task<List<User>> GetOnlineUsersAsync();
        
        /// <summary>
        /// Validates if a password meets requirements
        /// </summary>
        bool ValidatePassword(string password);
        
        /// <summary>
        /// Hashes a password
        /// </summary>
        string HashPassword(string password);
        
        /// <summary>
        /// Verifies a password against its hash
        /// </summary>
        bool VerifyPassword(string password, string hash);

        /// <summary>
        /// Deactivates a user account (admin only)
        /// </summary>
        Task<bool> DeactivateUserAsync(int userId, int deactivatedByUserId, string reason);

        /// <summary>
        /// Reactivates a user account (admin only)
        /// </summary>
        Task<bool> ReactivateUserAsync(int userId, int reactivatedByUserId);

        /// <summary>
        /// Deletes a user account permanently (admin only)
        /// </summary>
        Task<bool> DeleteUserAsync(int userId, int deletedByUserId);

        /// <summary>
        /// Gets all users for admin management
        /// </summary>
        Task<List<User>> GetAllUsersAsync(int currentUserId);

        /// <summary>
        /// Updates user profile information
        /// </summary>
        Task<User?> UpdateUserProfileAsync(int userId, string? displayName = null, string? email = null, string? bio = null, string? department = null, string? jobTitle = null, string? phoneNumber = null);

        /// <summary>
        /// Checks if user has admin role
        /// </summary>
        Task<bool> IsAdminAsync(int userId);

        /// <summary>
        /// Updates user's profile picture
        /// </summary>
        Task<bool> UpdateProfilePictureAsync(int userId, string profilePicturePath);
    }
}
