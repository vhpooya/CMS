using Microsoft.EntityFrameworkCore;
using RemoteDesktopApp.Data;
using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public class UnitService : IUnitService
    {
        private readonly RemoteDesktopDbContext _context;
        private readonly ILogger<UnitService> _logger;

        public UnitService(RemoteDesktopDbContext context, ILogger<UnitService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Unit>> GetAllUnitsAsync()
        {
            return await _context.Units
                .Include(u => u.ParentUnit)
                .Include(u => u.Manager)
                .Include(u => u.Users)
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<Unit?> GetUnitByIdAsync(int unitId)
        {
            return await _context.Units
                .Include(u => u.ParentUnit)
                .Include(u => u.SubUnits)
                .Include(u => u.Manager)
                .Include(u => u.Users)
                .Include(u => u.LinkedUnits).ThenInclude(l => l.TargetUnit)
                .Include(u => u.LinkedByUnits).ThenInclude(l => l.SourceUnit)
                .FirstOrDefaultAsync(u => u.Id == unitId && u.IsActive);
        }

        public async Task<Unit?> GetUnitByCodeAsync(string code)
        {
            return await _context.Units
                .Include(u => u.ParentUnit)
                .Include(u => u.Manager)
                .Include(u => u.Users)
                .FirstOrDefaultAsync(u => u.Code == code && u.IsActive);
        }

        public async Task<List<Unit>> GetSubUnitsAsync(int parentUnitId)
        {
            return await _context.Units
                .Include(u => u.Manager)
                .Include(u => u.Users)
                .Where(u => u.ParentUnitId == parentUnitId && u.IsActive)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<List<Unit>> GetRootUnitsAsync()
        {
            return await _context.Units
                .Include(u => u.Manager)
                .Include(u => u.Users)
                .Where(u => u.ParentUnitId == null && u.IsActive)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<Unit> CreateUnitAsync(Unit unit)
        {
            unit.CreatedAt = DateTime.UtcNow;
            unit.IsActive = true;

            _context.Units.Add(unit);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Unit created: {unit.Name} (ID: {unit.Id})");
            return unit;
        }

        public async Task<Unit> UpdateUnitAsync(Unit unit)
        {
            unit.UpdatedAt = DateTime.UtcNow;
            _context.Units.Update(unit);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Unit updated: {unit.Name} (ID: {unit.Id})");
            return unit;
        }

        public async Task<bool> DeleteUnitAsync(int unitId)
        {
            var unit = await _context.Units.FindAsync(unitId);
            if (unit == null) return false;

            // Check if unit can be deleted
            if (!await CanDeleteUnitAsync(unitId))
                return false;

            unit.IsActive = false;
            unit.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Unit deleted: {unit.Name} (ID: {unit.Id})");
            return true;
        }

        public async Task<bool> CanDeleteUnitAsync(int unitId)
        {
            // Check if unit has sub-units
            var hasSubUnits = await _context.Units.AnyAsync(u => u.ParentUnitId == unitId && u.IsActive);
            if (hasSubUnits) return false;

            // Check if unit has users
            var hasUsers = await _context.Users.AnyAsync(u => u.UnitId == unitId && u.IsActive);
            if (hasUsers) return false;

            return true;
        }

        public async Task<List<Unit>> GetUnitHierarchyAsync(int unitId)
        {
            var hierarchy = new List<Unit>();
            var currentUnit = await GetUnitByIdAsync(unitId);

            while (currentUnit != null)
            {
                hierarchy.Insert(0, currentUnit);
                currentUnit = currentUnit.ParentUnit;
            }

            return hierarchy;
        }

        public async Task<List<Unit>> GetParentUnitsAsync(int unitId)
        {
            var parents = new List<Unit>();
            var currentUnit = await _context.Units
                .Include(u => u.ParentUnit)
                .FirstOrDefaultAsync(u => u.Id == unitId);

            while (currentUnit?.ParentUnit != null)
            {
                parents.Insert(0, currentUnit.ParentUnit);
                currentUnit = currentUnit.ParentUnit;
            }

            return parents;
        }

        public async Task<bool> IsSubUnitOfAsync(int childUnitId, int parentUnitId)
        {
            var childUnit = await _context.Units
                .Include(u => u.ParentUnit)
                .FirstOrDefaultAsync(u => u.Id == childUnitId);

            while (childUnit?.ParentUnit != null)
            {
                if (childUnit.ParentUnit.Id == parentUnitId)
                    return true;
                childUnit = childUnit.ParentUnit;
            }

            return false;
        }

        public async Task<List<User>> GetUnitUsersAsync(int unitId)
        {
            return await _context.Users
                .Where(u => u.UnitId == unitId && u.IsActive)
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        public async Task<bool> AssignUserToUnitAsync(int userId, int unitId)
        {
            var user = await _context.Users.FindAsync(userId);
            var unit = await _context.Units.FindAsync(unitId);

            if (user == null || unit == null || !unit.IsActive)
                return false;

            user.UnitId = unitId;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {user.Username} assigned to unit {unit.Name}");
            return true;
        }

        public async Task<bool> RemoveUserFromUnitAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.UnitId = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {user.Username} removed from unit");
            return true;
        }

        public async Task<Unit?> GetUserUnitAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Unit)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.Unit;
        }

        public async Task<List<UnitLink>> GetUnitLinksAsync(int unitId)
        {
            return await _context.UnitLinks
                .Include(l => l.SourceUnit)
                .Include(l => l.TargetUnit)
                .Include(l => l.CreatedByUser)
                .Where(l => (l.SourceUnitId == unitId || l.TargetUnitId == unitId) && l.IsActive)
                .ToListAsync();
        }

        public async Task<UnitLink> CreateUnitLinkAsync(UnitLink unitLink)
        {
            unitLink.CreatedAt = DateTime.UtcNow;
            unitLink.IsActive = true;

            _context.UnitLinks.Add(unitLink);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Unit link created between {unitLink.SourceUnitId} and {unitLink.TargetUnitId}");
            return unitLink;
        }

        public async Task<bool> RemoveUnitLinkAsync(int linkId)
        {
            var link = await _context.UnitLinks.FindAsync(linkId);
            if (link == null) return false;

            link.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Unit link removed: {linkId}");
            return true;
        }

        public async Task<List<Unit>> GetLinkedUnitsAsync(int unitId)
        {
            var linkedUnitIds = await _context.UnitLinks
                .Where(l => l.SourceUnitId == unitId && l.IsActive)
                .Select(l => l.TargetUnitId)
                .ToListAsync();

            return await _context.Units
                .Where(u => linkedUnitIds.Contains(u.Id) && u.IsActive)
                .ToListAsync();
        }

        public async Task<bool> AreUnitsLinkedAsync(int sourceUnitId, int targetUnitId)
        {
            return await _context.UnitLinks
                .AnyAsync(l => l.SourceUnitId == sourceUnitId && l.TargetUnitId == targetUnitId && l.IsActive);
        }

        public async Task<List<UnitCommunicationPermission>> GetCommunicationPermissionsAsync(int unitId)
        {
            return await _context.UnitCommunicationPermissions
                .Include(p => p.SourceUnit)
                .Include(p => p.TargetUnit)
                .Include(p => p.CreatedByUser)
                .Where(p => p.SourceUnitId == unitId && p.IsAllowed)
                .ToListAsync();
        }

        public async Task<UnitCommunicationPermission> CreateCommunicationPermissionAsync(UnitCommunicationPermission permission)
        {
            permission.CreatedAt = DateTime.UtcNow;
            permission.IsAllowed = true;

            _context.UnitCommunicationPermissions.Add(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Communication permission created between units {permission.SourceUnitId} and {permission.TargetUnitId}");
            return permission;
        }

        public async Task<bool> RemoveCommunicationPermissionAsync(int permissionId)
        {
            var permission = await _context.UnitCommunicationPermissions.FindAsync(permissionId);
            if (permission == null) return false;

            _context.UnitCommunicationPermissions.Remove(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Communication permission removed: {permissionId}");
            return true;
        }

        public async Task<bool> CanCommunicateAsync(int sourceUnitId, int targetUnitId, CommunicationType communicationType)
        {
            // Same unit can always communicate
            if (sourceUnitId == targetUnitId) return true;

            // Check if units are linked
            var areLinked = await AreUnitsLinkedAsync(sourceUnitId, targetUnitId);
            if (areLinked) return true;

            // Check specific communication permission
            var hasPermission = await _context.UnitCommunicationPermissions
                .AnyAsync(p => p.SourceUnitId == sourceUnitId && 
                              p.TargetUnitId == targetUnitId && 
                              (p.CommunicationType == communicationType || p.CommunicationType == CommunicationType.All) &&
                              p.IsAllowed);

            return hasPermission;
        }

        public async Task<List<Unit>> GetAllowedCommunicationUnitsAsync(int unitId, CommunicationType communicationType)
        {
            // Get linked units
            var linkedUnits = await GetLinkedUnitsAsync(unitId);

            // Get units with specific communication permissions
            var permittedUnitIds = await _context.UnitCommunicationPermissions
                .Where(p => p.SourceUnitId == unitId && 
                           (p.CommunicationType == communicationType || p.CommunicationType == CommunicationType.All) &&
                           p.IsAllowed)
                .Select(p => p.TargetUnitId)
                .ToListAsync();

            var permittedUnits = await _context.Units
                .Where(u => permittedUnitIds.Contains(u.Id) && u.IsActive)
                .ToListAsync();

            // Combine and remove duplicates
            var allAllowedUnits = linkedUnits.Concat(permittedUnits)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .ToList();

            return allAllowedUnits;
        }

        public async Task<bool> CanUsersCommunicateAsync(int sourceUserId, int targetUserId, CommunicationType communicationType)
        {
            var sourceUserUnit = await GetUserUnitAsync(sourceUserId);
            var targetUserUnit = await GetUserUnitAsync(targetUserId);

            if (sourceUserUnit == null || targetUserUnit == null)
                return false;

            return await CanCommunicateAsync(sourceUserUnit.Id, targetUserUnit.Id, communicationType);
        }

        public async Task<List<User>> GetCommunicableUsersAsync(int userId, CommunicationType communicationType)
        {
            var userUnit = await GetUserUnitAsync(userId);
            if (userUnit == null) return new List<User>();

            var allowedUnits = await GetAllowedCommunicationUnitsAsync(userUnit.Id, communicationType);
            var allowedUnitIds = allowedUnits.Select(u => u.Id).ToList();
            allowedUnitIds.Add(userUnit.Id); // Add own unit

            return await _context.Users
                .Where(u => u.UnitId.HasValue && allowedUnitIds.Contains(u.UnitId.Value) && u.IsActive && u.Id != userId)
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        public async Task<int> GetUnitUserCountAsync(int unitId)
        {
            return await _context.Users.CountAsync(u => u.UnitId == unitId && u.IsActive);
        }

        public async Task<int> GetTotalUnitsCountAsync()
        {
            return await _context.Units.CountAsync(u => u.IsActive);
        }

        public async Task<Dictionary<int, int>> GetUnitsUserCountAsync()
        {
            return await _context.Users
                .Where(u => u.UnitId.HasValue && u.IsActive)
                .GroupBy(u => u.UnitId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<List<Unit>> SearchUnitsAsync(string searchTerm)
        {
            return await _context.Units
                .Include(u => u.ParentUnit)
                .Include(u => u.Manager)
                .Where(u => u.IsActive && 
                           (u.Name.Contains(searchTerm) || 
                            u.Code.Contains(searchTerm) || 
                            u.Description.Contains(searchTerm)))
                .OrderBy(u => u.Name)
                .ToListAsync();
        }
    }
}
