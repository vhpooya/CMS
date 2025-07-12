using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public interface IPermissionService
    {
        /// <summary>
        /// Checks if a user has a specific permission
        /// </summary>
        Task<bool> HasPermissionAsync(int userId, string permissionKey);
        
        /// <summary>
        /// Gets all permissions for a user (direct + group permissions)
        /// </summary>
        Task<List<Permission>> GetUserPermissionsAsync(int userId);
        
        /// <summary>
        /// Gets all available permissions
        /// </summary>
        Task<List<Permission>> GetAllPermissionsAsync();
        
        /// <summary>
        /// Creates a new user group
        /// </summary>
        Task<UserGroup> CreateGroupAsync(int creatorId, string name, string? description = null, string? color = null, string? icon = null);
        
        /// <summary>
        /// Updates a user group
        /// </summary>
        Task<UserGroup?> UpdateGroupAsync(int groupId, int updatedByUserId, string? name = null, string? description = null, string? color = null, string? icon = null);
        
        /// <summary>
        /// Deletes a user group
        /// </summary>
        Task<bool> DeleteGroupAsync(int groupId, int deletedByUserId);
        
        /// <summary>
        /// Gets all user groups
        /// </summary>
        Task<List<UserGroup>> GetAllGroupsAsync();
        
        /// <summary>
        /// Gets groups for a specific user
        /// </summary>
        Task<List<UserGroup>> GetUserGroupsAsync(int userId);
        
        /// <summary>
        /// Adds a user to a group
        /// </summary>
        Task<UserGroupMember> AddUserToGroupAsync(int groupId, int userId, int addedByUserId, GroupMemberRole role = GroupMemberRole.Member);
        
        /// <summary>
        /// Removes a user from a group
        /// </summary>
        Task<bool> RemoveUserFromGroupAsync(int groupId, int userId, int removedByUserId);
        
        /// <summary>
        /// Updates user role in a group
        /// </summary>
        Task<bool> UpdateGroupMemberRoleAsync(int groupId, int userId, int updatedByUserId, GroupMemberRole newRole);
        
        /// <summary>
        /// Gets all members of a group
        /// </summary>
        Task<List<UserGroupMember>> GetGroupMembersAsync(int groupId);
        
        /// <summary>
        /// Grants permission to a group
        /// </summary>
        Task<GroupPermission> GrantPermissionToGroupAsync(int groupId, int permissionId, int grantedByUserId);
        
        /// <summary>
        /// Revokes permission from a group
        /// </summary>
        Task<bool> RevokePermissionFromGroupAsync(int groupId, int permissionId, int revokedByUserId);
        
        /// <summary>
        /// Grants permission directly to a user
        /// </summary>
        Task<UserPermission> GrantPermissionToUserAsync(int userId, int permissionId, int grantedByUserId);
        
        /// <summary>
        /// Revokes permission directly from a user
        /// </summary>
        Task<bool> RevokePermissionFromUserAsync(int userId, int permissionId, int revokedByUserId);
        
        /// <summary>
        /// Gets permissions for a specific group
        /// </summary>
        Task<List<Permission>> GetGroupPermissionsAsync(int groupId);
        
        /// <summary>
        /// Initializes default permissions and groups
        /// </summary>
        Task InitializeDefaultPermissionsAsync();
        
        /// <summary>
        /// Gets users without any group membership
        /// </summary>
        Task<List<User>> GetUsersWithoutGroupAsync();
        
        /// <summary>
        /// Bulk adds users to a group
        /// </summary>
        Task<List<UserGroupMember>> BulkAddUsersToGroupAsync(int groupId, List<int> userIds, int addedByUserId, GroupMemberRole role = GroupMemberRole.Member);
        
        /// <summary>
        /// Bulk grants permissions to a group
        /// </summary>
        Task<List<GroupPermission>> BulkGrantPermissionsToGroupAsync(int groupId, List<int> permissionIds, int grantedByUserId);
        
        /// <summary>
        /// Gets permission summary for admin dashboard
        /// </summary>
        Task<PermissionSummary> GetPermissionSummaryAsync();
        
        /// <summary>
        /// Checks if user can manage groups (admin permission)
        /// </summary>
        Task<bool> CanManageGroupsAsync(int userId);
        
        /// <summary>
        /// Checks if user can manage permissions (admin permission)
        /// </summary>
        Task<bool> CanManagePermissionsAsync(int userId);
        
        /// <summary>
        /// Gets module permissions for a user (for UI filtering)
        /// </summary>
        Task<Dictionary<string, List<string>>> GetUserModulePermissionsAsync(int userId);
    }
    
    public class PermissionSummary
    {
        public int TotalGroups { get; set; }
        public int TotalPermissions { get; set; }
        public int TotalUsers { get; set; }
        public int UsersWithGroups { get; set; }
        public int UsersWithoutGroups { get; set; }
        public List<GroupSummary> GroupSummaries { get; set; } = new();
        public List<ModulePermissionSummary> ModuleSummaries { get; set; } = new();
    }
    
    public class GroupSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int MemberCount { get; set; }
        public int PermissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public bool IsSystemGroup { get; set; }
    }
    
    public class ModulePermissionSummary
    {
        public string Module { get; set; } = string.Empty;
        public int TotalPermissions { get; set; }
        public int GroupsWithAccess { get; set; }
        public int UsersWithAccess { get; set; }
        public List<string> Actions { get; set; } = new();
    }
}
