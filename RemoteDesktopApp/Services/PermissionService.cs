using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly RemoteDesktopDbContext _context;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(RemoteDesktopDbContext context, ILogger<PermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionKey)
        {
            // Check if user is admin (admins have all permissions)
            var user = await _context.Users.FindAsync(userId);
            if (user?.Role == UserRole.Admin)
                return true;

            // Check direct user permissions
            var hasDirectPermission = await _context.UserPermissions
                .Include(up => up.Permission)
                .AnyAsync(up => up.UserId == userId && 
                               up.Permission.Key == permissionKey && 
                               up.IsActive);

            if (hasDirectPermission)
                return true;

            // Check group permissions
            var hasGroupPermission = await _context.UserGroupMembers
                .Include(ugm => ugm.Group)
                .ThenInclude(g => g.Permissions)
                .ThenInclude(gp => gp.Permission)
                .AnyAsync(ugm => ugm.UserId == userId && 
                                ugm.IsActive && 
                                ugm.Group.IsActive &&
                                ugm.Group.Permissions.Any(gp => gp.Permission.Key == permissionKey && gp.IsActive));

            return hasGroupPermission;
        }

        public async Task<List<Permission>> GetUserPermissionsAsync(int userId)
        {
            var permissions = new List<Permission>();

            // Get direct permissions
            var directPermissions = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId && up.IsActive)
                .Select(up => up.Permission)
                .ToListAsync();

            permissions.AddRange(directPermissions);

            // Get group permissions
            var groupPermissions = await _context.UserGroupMembers
                .Include(ugm => ugm.Group)
                .ThenInclude(g => g.Permissions)
                .ThenInclude(gp => gp.Permission)
                .Where(ugm => ugm.UserId == userId && ugm.IsActive && ugm.Group.IsActive)
                .SelectMany(ugm => ugm.Group.Permissions.Where(gp => gp.IsActive).Select(gp => gp.Permission))
                .ToListAsync();

            permissions.AddRange(groupPermissions);

            // Remove duplicates
            return permissions.GroupBy(p => p.Id).Select(g => g.First()).ToList();
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            return await _context.Permissions
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Action)
                .ToListAsync();
        }

        public async Task<UserGroup> CreateGroupAsync(int creatorId, string name, string? description = null, string? color = null, string? icon = null)
        {
            var group = new UserGroup
            {
                Name = name,
                Description = description,
                Color = color,
                Icon = icon,
                CreatedByUserId = creatorId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserGroups.Add(group);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User group {GroupName} created by user {CreatorId}", name, creatorId);
            return group;
        }

        public async Task<UserGroup?> UpdateGroupAsync(int groupId, int updatedByUserId, string? name = null, string? description = null, string? color = null, string? icon = null)
        {
            var group = await _context.UserGroups.FindAsync(groupId);
            if (group == null || group.IsSystemGroup)
                return null;

            if (!string.IsNullOrEmpty(name))
                group.Name = name;
            
            if (description != null)
                group.Description = description;
            
            if (!string.IsNullOrEmpty(color))
                group.Color = color;
            
            if (!string.IsNullOrEmpty(icon))
                group.Icon = icon;

            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return group;
        }

        public async Task<bool> DeleteGroupAsync(int groupId, int deletedByUserId)
        {
            var group = await _context.UserGroups.FindAsync(groupId);
            if (group == null || group.IsSystemGroup)
                return false;

            group.IsActive = false;
            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<UserGroup>> GetAllGroupsAsync()
        {
            return await _context.UserGroups
                .Include(g => g.CreatedBy)
                .Include(g => g.Members.Where(m => m.IsActive))
                .ThenInclude(m => m.User)
                .Include(g => g.Permissions.Where(p => p.IsActive))
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        public async Task<List<UserGroup>> GetUserGroupsAsync(int userId)
        {
            return await _context.UserGroupMembers
                .Include(ugm => ugm.Group)
                .ThenInclude(g => g.CreatedBy)
                .Where(ugm => ugm.UserId == userId && ugm.IsActive && ugm.Group.IsActive)
                .Select(ugm => ugm.Group)
                .ToListAsync();
        }

        public async Task<UserGroupMember> AddUserToGroupAsync(int groupId, int userId, int addedByUserId, GroupMemberRole role = GroupMemberRole.Member)
        {
            // Check if user is already in group
            var existingMember = await _context.UserGroupMembers
                .FirstOrDefaultAsync(ugm => ugm.GroupId == groupId && ugm.UserId == userId);

            if (existingMember != null)
            {
                if (existingMember.IsActive)
                    throw new InvalidOperationException("User is already a member of this group");
                
                // Reactivate membership
                existingMember.IsActive = true;
                existingMember.AddedAt = DateTime.UtcNow;
                existingMember.AddedByUserId = addedByUserId;
                existingMember.Role = role;
                
                await _context.SaveChangesAsync();
                return existingMember;
            }

            var member = new UserGroupMember
            {
                GroupId = groupId,
                UserId = userId,
                AddedByUserId = addedByUserId,
                Role = role,
                AddedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserGroupMembers.Add(member);
            await _context.SaveChangesAsync();

            return member;
        }

        public async Task<bool> RemoveUserFromGroupAsync(int groupId, int userId, int removedByUserId)
        {
            var member = await _context.UserGroupMembers
                .FirstOrDefaultAsync(ugm => ugm.GroupId == groupId && ugm.UserId == userId && ugm.IsActive);

            if (member == null)
                return false;

            member.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateGroupMemberRoleAsync(int groupId, int userId, int updatedByUserId, GroupMemberRole newRole)
        {
            var member = await _context.UserGroupMembers
                .FirstOrDefaultAsync(ugm => ugm.GroupId == groupId && ugm.UserId == userId && ugm.IsActive);

            if (member == null)
                return false;

            member.Role = newRole;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<UserGroupMember>> GetGroupMembersAsync(int groupId)
        {
            return await _context.UserGroupMembers
                .Include(ugm => ugm.User)
                .Include(ugm => ugm.AddedBy)
                .Where(ugm => ugm.GroupId == groupId && ugm.IsActive)
                .OrderBy(ugm => ugm.User.DisplayName)
                .ToListAsync();
        }

        public async Task<GroupPermission> GrantPermissionToGroupAsync(int groupId, int permissionId, int grantedByUserId)
        {
            // Check if permission already exists
            var existingPermission = await _context.GroupPermissions
                .FirstOrDefaultAsync(gp => gp.GroupId == groupId && gp.PermissionId == permissionId);

            if (existingPermission != null)
            {
                if (existingPermission.IsActive)
                    return existingPermission;
                
                // Reactivate permission
                existingPermission.IsActive = true;
                existingPermission.GrantedAt = DateTime.UtcNow;
                existingPermission.GrantedByUserId = grantedByUserId;
                
                await _context.SaveChangesAsync();
                return existingPermission;
            }

            var groupPermission = new GroupPermission
            {
                GroupId = groupId,
                PermissionId = permissionId,
                GrantedByUserId = grantedByUserId,
                GrantedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.GroupPermissions.Add(groupPermission);
            await _context.SaveChangesAsync();

            return groupPermission;
        }

        public async Task<bool> RevokePermissionFromGroupAsync(int groupId, int permissionId, int revokedByUserId)
        {
            var groupPermission = await _context.GroupPermissions
                .FirstOrDefaultAsync(gp => gp.GroupId == groupId && gp.PermissionId == permissionId && gp.IsActive);

            if (groupPermission == null)
                return false;

            groupPermission.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<UserPermission> GrantPermissionToUserAsync(int userId, int permissionId, int grantedByUserId)
        {
            // Check if permission already exists
            var existingPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

            if (existingPermission != null)
            {
                if (existingPermission.IsActive)
                    return existingPermission;
                
                // Reactivate permission
                existingPermission.IsActive = true;
                existingPermission.GrantedAt = DateTime.UtcNow;
                existingPermission.GrantedByUserId = grantedByUserId;
                
                await _context.SaveChangesAsync();
                return existingPermission;
            }

            var userPermission = new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                GrantedByUserId = grantedByUserId,
                GrantedAt = DateTime.UtcNow,
                Source = PermissionSource.Direct,
                IsActive = true
            };

            _context.UserPermissions.Add(userPermission);
            await _context.SaveChangesAsync();

            return userPermission;
        }

        public async Task<bool> RevokePermissionFromUserAsync(int userId, int permissionId, int revokedByUserId)
        {
            var userPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId && up.IsActive);

            if (userPermission == null)
                return false;

            userPermission.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Permission>> GetGroupPermissionsAsync(int groupId)
        {
            return await _context.GroupPermissions
                .Include(gp => gp.Permission)
                .Where(gp => gp.GroupId == groupId && gp.IsActive)
                .Select(gp => gp.Permission)
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Action)
                .ToListAsync();
        }

        public async Task InitializeDefaultPermissionsAsync()
        {
            // Check if permissions already exist
            if (await _context.Permissions.AnyAsync())
                return;

            var permissions = new List<Permission>
            {
                // Dashboard permissions
                new Permission { Name = "View Dashboard", Key = "dashboard.view", Module = "dashboard", Action = "view", Level = PermissionLevel.Basic, IsSystemPermission = true },

                // Calendar permissions
                new Permission { Name = "View Calendar", Key = "calendar.view", Module = "calendar", Action = "view", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Create Events", Key = "calendar.create", Module = "calendar", Action = "create", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Edit Events", Key = "calendar.edit", Module = "calendar", Action = "edit", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Delete Events", Key = "calendar.delete", Module = "calendar", Action = "delete", Level = PermissionLevel.Intermediate, IsSystemPermission = true },
                new Permission { Name = "Manage Calendar", Key = "calendar.admin", Module = "calendar", Action = "admin", Level = PermissionLevel.Admin, IsSystemPermission = true },

                // Spreadsheet permissions
                new Permission { Name = "View Spreadsheets", Key = "spreadsheets.view", Module = "spreadsheets", Action = "view", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Create Spreadsheets", Key = "spreadsheets.create", Module = "spreadsheets", Action = "create", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Edit Spreadsheets", Key = "spreadsheets.edit", Module = "spreadsheets", Action = "edit", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Delete Spreadsheets", Key = "spreadsheets.delete", Module = "spreadsheets", Action = "delete", Level = PermissionLevel.Intermediate, IsSystemPermission = true },
                new Permission { Name = "Share Spreadsheets", Key = "spreadsheets.share", Module = "spreadsheets", Action = "share", Level = PermissionLevel.Intermediate, IsSystemPermission = true },
                new Permission { Name = "Manage Spreadsheets", Key = "spreadsheets.admin", Module = "spreadsheets", Action = "admin", Level = PermissionLevel.Admin, IsSystemPermission = true },

                // Messages permissions
                new Permission { Name = "View Messages", Key = "messages.view", Module = "messages", Action = "view", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Send Messages", Key = "messages.send", Module = "messages", Action = "send", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Create Groups", Key = "messages.create_groups", Module = "messages", Action = "create_groups", Level = PermissionLevel.Intermediate, IsSystemPermission = true },
                new Permission { Name = "Manage Messages", Key = "messages.admin", Module = "messages", Action = "admin", Level = PermissionLevel.Admin, IsSystemPermission = true },

                // Code Editor permissions
                new Permission { Name = "View Code Editor", Key = "code.view", Module = "code", Action = "view", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Execute Code", Key = "code.execute", Module = "code", Action = "execute", Level = PermissionLevel.Intermediate, IsSystemPermission = true },
                new Permission { Name = "Save Code", Key = "code.save", Module = "code", Action = "save", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Share Code", Key = "code.share", Module = "code", Action = "share", Level = PermissionLevel.Intermediate, IsSystemPermission = true },
                new Permission { Name = "Manage Code Library", Key = "code.admin", Module = "code", Action = "admin", Level = PermissionLevel.Admin, IsSystemPermission = true },

                // Remote Desktop permissions
                new Permission { Name = "View Remote Desktop", Key = "remote.view", Module = "remote", Action = "view", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Control Remote Desktop", Key = "remote.control", Module = "remote", Action = "control", Level = PermissionLevel.Intermediate, IsSystemPermission = true },
                new Permission { Name = "File Transfer", Key = "remote.file_transfer", Module = "remote", Action = "file_transfer", Level = PermissionLevel.Intermediate, IsSystemPermission = true },
                new Permission { Name = "Manage Remote Connections", Key = "remote.admin", Module = "remote", Action = "admin", Level = PermissionLevel.Admin, IsSystemPermission = true },

                // Admin permissions
                new Permission { Name = "View Admin Panel", Key = "admin.view", Module = "admin", Action = "view", Level = PermissionLevel.Admin, IsSystemPermission = true },
                new Permission { Name = "Manage Users", Key = "admin.users", Module = "admin", Action = "users", Level = PermissionLevel.Admin, IsSystemPermission = true },
                new Permission { Name = "Manage Groups", Key = "admin.groups", Module = "admin", Action = "groups", Level = PermissionLevel.Admin, IsSystemPermission = true },
                new Permission { Name = "Manage Permissions", Key = "admin.permissions", Module = "admin", Action = "permissions", Level = PermissionLevel.Admin, IsSystemPermission = true },
                new Permission { Name = "System Settings", Key = "admin.settings", Module = "admin", Action = "settings", Level = PermissionLevel.Admin, IsSystemPermission = true },

                // Profile permissions
                new Permission { Name = "View Profile", Key = "profile.view", Module = "profile", Action = "view", Level = PermissionLevel.Basic, IsSystemPermission = true },
                new Permission { Name = "Edit Profile", Key = "profile.edit", Module = "profile", Action = "edit", Level = PermissionLevel.Basic, IsSystemPermission = true }
            };

            _context.Permissions.AddRange(permissions);
            await _context.SaveChangesAsync();

            // Create default groups
            var adminGroup = new UserGroup
            {
                Name = "Administrators",
                Description = "Full system access",
                Color = "#dc3545",
                Icon = "fas fa-shield-alt",
                CreatedByUserId = 1, // Assuming admin user has ID 1
                IsSystemGroup = true,
                IsActive = true
            };

            var userGroup = new UserGroup
            {
                Name = "Standard Users",
                Description = "Basic user access",
                Color = "#007bff",
                Icon = "fas fa-users",
                CreatedByUserId = 1,
                IsSystemGroup = true,
                IsActive = true
            };

            _context.UserGroups.AddRange(adminGroup, userGroup);
            await _context.SaveChangesAsync();

            // Grant all permissions to admin group
            var allPermissions = await _context.Permissions.ToListAsync();
            var adminGroupPermissions = allPermissions.Select(p => new GroupPermission
            {
                GroupId = adminGroup.Id,
                PermissionId = p.Id,
                GrantedByUserId = 1,
                IsActive = true
            }).ToList();

            _context.GroupPermissions.AddRange(adminGroupPermissions);

            // Grant basic permissions to user group
            var basicPermissions = allPermissions.Where(p => p.Level == PermissionLevel.Basic).ToList();
            var userGroupPermissions = basicPermissions.Select(p => new GroupPermission
            {
                GroupId = userGroup.Id,
                PermissionId = p.Id,
                GrantedByUserId = 1,
                IsActive = true
            }).ToList();

            _context.GroupPermissions.AddRange(userGroupPermissions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Default permissions and groups initialized");
        }

        public async Task<List<User>> GetUsersWithoutGroupAsync()
        {
            var usersWithGroups = await _context.UserGroupMembers
                .Where(ugm => ugm.IsActive)
                .Select(ugm => ugm.UserId)
                .Distinct()
                .ToListAsync();

            return await _context.Users
                .Where(u => u.IsActive && !u.IsDeactivated && !usersWithGroups.Contains(u.Id))
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        public async Task<List<UserGroupMember>> BulkAddUsersToGroupAsync(int groupId, List<int> userIds, int addedByUserId, GroupMemberRole role = GroupMemberRole.Member)
        {
            var members = new List<UserGroupMember>();

            foreach (var userId in userIds)
            {
                try
                {
                    var member = await AddUserToGroupAsync(groupId, userId, addedByUserId, role);
                    members.Add(member);
                }
                catch (InvalidOperationException)
                {
                    // User already in group, skip
                    continue;
                }
            }

            return members;
        }

        public async Task<List<GroupPermission>> BulkGrantPermissionsToGroupAsync(int groupId, List<int> permissionIds, int grantedByUserId)
        {
            var permissions = new List<GroupPermission>();

            foreach (var permissionId in permissionIds)
            {
                var permission = await GrantPermissionToGroupAsync(groupId, permissionId, grantedByUserId);
                permissions.Add(permission);
            }

            return permissions;
        }

        public async Task<PermissionSummary> GetPermissionSummaryAsync()
        {
            var totalGroups = await _context.UserGroups.CountAsync(g => g.IsActive);
            var totalPermissions = await _context.Permissions.CountAsync();
            var totalUsers = await _context.Users.CountAsync(u => u.IsActive && !u.IsDeactivated);

            var usersWithGroups = await _context.UserGroupMembers
                .Where(ugm => ugm.IsActive)
                .Select(ugm => ugm.UserId)
                .Distinct()
                .CountAsync();

            var groupSummaries = await _context.UserGroups
                .Include(g => g.CreatedBy)
                .Where(g => g.IsActive)
                .Select(g => new GroupSummary
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    Color = g.Color,
                    Icon = g.Icon,
                    MemberCount = g.Members.Count(m => m.IsActive),
                    PermissionCount = g.Permissions.Count(p => p.IsActive),
                    CreatedAt = g.CreatedAt,
                    CreatedByName = g.CreatedBy.DisplayName,
                    IsSystemGroup = g.IsSystemGroup
                })
                .ToListAsync();

            var moduleSummaries = await _context.Permissions
                .GroupBy(p => p.Module)
                .Select(g => new ModulePermissionSummary
                {
                    Module = g.Key,
                    TotalPermissions = g.Count(),
                    Actions = g.Select(p => p.Action).Distinct().ToList()
                })
                .ToListAsync();

            return new PermissionSummary
            {
                TotalGroups = totalGroups,
                TotalPermissions = totalPermissions,
                TotalUsers = totalUsers,
                UsersWithGroups = usersWithGroups,
                UsersWithoutGroups = totalUsers - usersWithGroups,
                GroupSummaries = groupSummaries,
                ModuleSummaries = moduleSummaries
            };
        }

        public async Task<bool> CanManageGroupsAsync(int userId)
        {
            return await HasPermissionAsync(userId, "admin.groups");
        }

        public async Task<bool> CanManagePermissionsAsync(int userId)
        {
            return await HasPermissionAsync(userId, "admin.permissions");
        }

        public async Task<Dictionary<string, List<string>>> GetUserModulePermissionsAsync(int userId)
        {
            var permissions = await GetUserPermissionsAsync(userId);

            return permissions
                .GroupBy(p => p.Module)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => p.Action).ToList()
                );
        }
    }
}
