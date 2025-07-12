using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;
using Microsoft.EntityFrameworkCore;

namespace RemoteDesktopApp.Services
{
    public class DataSeedingService
    {
        private readonly RemoteDesktopDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<DataSeedingService> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IMobilePhoneService _mobilePhoneService;

        public DataSeedingService(RemoteDesktopDbContext context, IUserService userService, ILogger<DataSeedingService> logger, IPermissionService permissionService, IMobilePhoneService mobilePhoneService)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
            _permissionService = permissionService;
            _mobilePhoneService = mobilePhoneService;
        }

        public async Task SeedDataAsync()
        {
            try
            {
                // Initialize permissions first
                await _permissionService.InitializeDefaultPermissionsAsync();

                // Always assign phone numbers to users who don't have them
                await AssignPhoneNumbersAsync();

                // Seed units
                // await SeedUnitsAsync(); // Temporarily disabled

                // Check if admin user exists, create if missing
                var adminExists = await _context.Users.AnyAsync(u => u.Role == UserRole.Admin);
                if (adminExists)
                {
                    _logger.LogInformation("Admin user already exists, skipping user creation");
                    return;
                }

                _logger.LogInformation("Seeding initial data...");

                // Create demo users
                var demoUsers = new[]
                {
                    new { Username = "admin", Email = "admin@example.com", Password = "admin123", DisplayName = "Administrator", Role = UserRole.Admin, Department = "IT", JobTitle = "System Administrator" },
                    new { Username = "user", Email = "user@example.com", Password = "user123", DisplayName = "Demo User", Role = UserRole.User, Department = "General", JobTitle = "User" },
                    new { Username = "demo", Email = "demo@example.com", Password = "demo123", DisplayName = "Demo Account", Role = UserRole.User, Department = "Demo", JobTitle = "Demo User" },
                    new { Username = "alice", Email = "alice@example.com", Password = "alice123", DisplayName = "Alice Johnson", Role = UserRole.User, Department = "Marketing", JobTitle = "Marketing Manager" },
                    new { Username = "bob", Email = "bob@example.com", Password = "bob123", DisplayName = "Bob Smith", Role = UserRole.User, Department = "Development", JobTitle = "Software Developer" }
                };

                User? adminUser = null;
                foreach (var userData in demoUsers)
                {
                    try
                    {
                        var user = await _userService.CreateUserAsync(
                            userData.Username,
                            userData.Email,
                            userData.Password,
                            userData.DisplayName,
                            userData.Role,
                            adminUser?.Id // First user (admin) creates others
                        );

                        // Update additional profile information
                        await _userService.UpdateUserProfileAsync(
                            user.Id,
                            department: userData.Department,
                            jobTitle: userData.JobTitle
                        );

                        if (userData.Role == UserRole.Admin && adminUser == null)
                        {
                            adminUser = user;
                        }

                        _logger.LogInformation("Created {Role} user {Username} with client ID {ClientId}",
                            userData.Role, user.Username, user.ClientId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create user {Username}", userData.Username);
                    }
                }

                // Assign users to groups
                if (adminUser != null)
                {
                    var adminGroup = await _context.UserGroups.FirstOrDefaultAsync(g => g.Name == "Administrators");
                    if (adminGroup != null)
                    {
                        await _permissionService.AddUserToGroupAsync(adminGroup.Id, adminUser.Id, adminUser.Id, GroupMemberRole.Admin);
                        _logger.LogInformation("Added admin user to Administrators group");
                    }
                }

                // Add all other users to Standard Users group
                var standardGroup = await _context.UserGroups.FirstOrDefaultAsync(g => g.Name == "Standard Users");
                if (standardGroup != null)
                {
                    var allUsers = await _context.Users.Where(u => u.Role != UserRole.Admin).ToListAsync();
                    foreach (var user in allUsers)
                    {
                        try
                        {
                            await _permissionService.AddUserToGroupAsync(standardGroup.Id, user.Id, adminUser?.Id ?? 1, GroupMemberRole.Member);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to add user {UserId} to Standard Users group", user.Id);
                        }
                    }
                    _logger.LogInformation("Added {Count} users to Standard Users group", allUsers.Count);
                }

                _logger.LogInformation("Data seeding completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data seeding");
            }
        }

        private async Task AssignPhoneNumbersAsync()
        {
            try
            {
                // Get users without phone numbers
                var usersWithoutPhones = await _context.Users
                    .Where(u => string.IsNullOrEmpty(u.PhoneNumber))
                    .ToListAsync();

                if (usersWithoutPhones.Any())
                {
                    _logger.LogInformation("Found {Count} users without phone numbers, assigning...", usersWithoutPhones.Count);

                    foreach (var user in usersWithoutPhones)
                    {
                        try
                        {
                            var phoneNumber = await _mobilePhoneService.AssignPhoneNumberAsync(user.Id);
                            _logger.LogInformation("Assigned phone number {PhoneNumber} to user {UserName}", phoneNumber, user.DisplayName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to assign phone number to user {UserId}", user.Id);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("All users already have phone numbers assigned");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning phone numbers");
            }
        }

        private async Task SeedUnitsAsync()
        {
            try
            {
                // Check if units already exist
                if (await _context.Units.AnyAsync())
                {
                    _logger.LogInformation("Units already exist, skipping unit seeding");
                    return;
                }

                _logger.LogInformation("Seeding initial units...");

                // Get admin user for creating units
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
                if (adminUser == null)
                {
                    _logger.LogWarning("No admin user found, skipping unit seeding");
                    return;
                }

                // Create root units
                var managementUnit = new Unit
                {
                    Name = "مدیریت عامل",
                    Code = "MGT",
                    Description = "مدیریت عامل شرکت",
                    CreatedByUserId = adminUser.Id,
                    Location = "طبقه اول",
                    PhoneExtension = "100",
                    Email = "management@company.com",
                    ManagerId = adminUser.Id
                };

                var itUnit = new Unit
                {
                    Name = "واحد فناوری اطلاعات",
                    Code = "IT",
                    Description = "واحد فناوری اطلاعات و ارتباطات",
                    CreatedByUserId = adminUser.Id,
                    Location = "طبقه دوم",
                    PhoneExtension = "200",
                    Email = "it@company.com"
                };

                var financeUnit = new Unit
                {
                    Name = "واحد مالی",
                    Code = "FIN",
                    Description = "واحد مالی و حسابداری",
                    CreatedByUserId = adminUser.Id,
                    Location = "طبقه سوم",
                    PhoneExtension = "300",
                    Email = "finance@company.com"
                };

                _context.Units.AddRange(managementUnit, itUnit, financeUnit);
                await _context.SaveChangesAsync();

                // Assign admin to management unit
                adminUser.UnitId = managementUnit.Id;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully seeded {Count} units", 3);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding units");
            }
        }
    }
}
