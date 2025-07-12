using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;
using System.Security.Cryptography;
using System.Text;

namespace RemoteDesktopApp.Services
{
    public class UserService : IUserService
    {
        private readonly RemoteDesktopDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(RemoteDesktopDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

                if (user != null && VerifyPassword(password, user.PasswordHash))
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    user.LastSeenAt = DateTime.UtcNow;
                    user.IsOnline = true;
                    await _context.SaveChangesAsync();
                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating user {Username}", username);
                return null;
            }
        }

        public async Task<User> CreateUserAsync(string username, string email, string password, string displayName, UserRole role = UserRole.User, int? createdByUserId = null)
        {
            if (!ValidatePassword(password))
                throw new ArgumentException("Password does not meet requirements");

            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == username || u.Email == email);

            if (existingUser)
                throw new InvalidOperationException("Username or email already exists");

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                DisplayName = displayName,
                ClientId = await GenerateUniqueClientIdAsync(),
                Role = role,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsOnline = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user {Username} with client ID {ClientId}", username, user.ClientId);
            return user;
        }

        public async Task<User?> GetUserByClientIdAsync(string clientId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.ClientId == clientId && u.IsActive);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }

        public async Task UpdateUserOnlineStatusAsync(int userId, bool isOnline)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = isOnline;
                user.LastSeenAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateLastSeenAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastSeenAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GenerateUniqueClientIdAsync()
        {
            string clientId;
            bool exists;

            do
            {
                clientId = GenerateClientId();
                exists = await _context.Users.AnyAsync(u => u.ClientId == clientId);
            } while (exists);

            return clientId;
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm, int currentUserId)
        {
            return await _context.Users
                .Where(u => u.IsActive && u.Id != currentUserId &&
                           (u.DisplayName.Contains(searchTerm) || u.Username.Contains(searchTerm)))
                .OrderBy(u => u.DisplayName)
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<User>> GetOnlineUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsOnline && u.IsActive)
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        public bool ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Minimum 6 characters, at least one letter and one number
            return password.Length >= 6 &&
                   password.Any(char.IsLetter) &&
                   password.Any(char.IsDigit);
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var salt = "RemoteDesktopApp_Salt_2024"; // In production, use a random salt per user
            var saltedPassword = password + salt;
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }

        public async Task<bool> DeactivateUserAsync(int userId, int deactivatedByUserId, string reason)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.IsDeactivated)
                    return false;

                user.IsDeactivated = true;
                user.DeactivatedAt = DateTime.UtcNow;
                user.DeactivatedByUserId = deactivatedByUserId;
                user.DeactivationReason = reason;
                user.IsOnline = false;

                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} deactivated by {DeactivatedBy}", userId, deactivatedByUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ReactivateUserAsync(int userId, int reactivatedByUserId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsDeactivated)
                    return false;

                user.IsDeactivated = false;
                user.DeactivatedAt = null;
                user.DeactivatedByUserId = null;
                user.DeactivationReason = null;

                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} reactivated by {ReactivatedBy}", userId, reactivatedByUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId, int deletedByUserId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                // Soft delete by marking as inactive
                user.IsActive = false;
                user.IsDeactivated = true;
                user.DeactivatedAt = DateTime.UtcNow;
                user.DeactivatedByUserId = deletedByUserId;
                user.DeactivationReason = "Account deleted";
                user.IsOnline = false;

                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} deleted by {DeletedBy}", userId, deletedByUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return false;
            }
        }

        public async Task<List<User>> GetAllUsersAsync(int currentUserId)
        {
            return await _context.Users
                .Where(u => u.IsActive && u.Id != currentUserId)
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        public async Task<User?> UpdateUserProfileAsync(int userId, string? displayName = null, string? email = null, string? bio = null, string? department = null, string? jobTitle = null, string? phoneNumber = null)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return null;

                if (!string.IsNullOrEmpty(displayName))
                    user.DisplayName = displayName;
                if (!string.IsNullOrEmpty(email))
                    user.Email = email;
                if (bio != null)
                    user.Bio = bio;
                if (!string.IsNullOrEmpty(department))
                    user.Department = department;
                if (!string.IsNullOrEmpty(jobTitle))
                    user.JobTitle = jobTitle;
                if (!string.IsNullOrEmpty(phoneNumber))
                    user.PhoneNumber = phoneNumber;

                await _context.SaveChangesAsync();
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> IsAdminAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Role == UserRole.Admin;
        }

        public async Task<bool> UpdateProfilePictureAsync(int userId, string profilePicturePath)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                user.ProfilePicture = profilePicturePath;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile picture for user {UserId}", userId);
                return false;
            }
        }

        private string GenerateClientId()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new StringBuilder(12);

            for (int i = 0; i < 12; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();

           


        }

        public async Task<IEnumerable<User>> GetAllActiveUsersAsync()
        {
            return await _context.Users.Where(x => x.IsActive == true).ToListAsync();
        }
    }
}
