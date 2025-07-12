using RemoteDesktopApp.Models;

namespace RemoteDesktopApp.Services
{
    public interface IUnitService
    {
        // Unit management
        Task<List<Unit>> GetAllUnitsAsync();
        Task<Unit?> GetUnitByIdAsync(int unitId);
        Task<Unit?> GetUnitByCodeAsync(string code);
        Task<List<Unit>> GetSubUnitsAsync(int parentUnitId);
        Task<List<Unit>> GetRootUnitsAsync();
        Task<Unit> CreateUnitAsync(Unit unit);
        Task<Unit> UpdateUnitAsync(Unit unit);
        Task<bool> DeleteUnitAsync(int unitId);
        Task<bool> CanDeleteUnitAsync(int unitId);

        // Unit hierarchy
        Task<List<Unit>> GetUnitHierarchyAsync(int unitId);
        Task<List<Unit>> GetParentUnitsAsync(int unitId);
        Task<bool> IsSubUnitOfAsync(int childUnitId, int parentUnitId);

        // User-Unit management
        Task<List<User>> GetUnitUsersAsync(int unitId);
        Task<bool> AssignUserToUnitAsync(int userId, int unitId);
        Task<bool> RemoveUserFromUnitAsync(int userId);
        Task<Unit?> GetUserUnitAsync(int userId);

        // Unit linking
        Task<List<UnitLink>> GetUnitLinksAsync(int unitId);
        Task<UnitLink> CreateUnitLinkAsync(UnitLink unitLink);
        Task<bool> RemoveUnitLinkAsync(int linkId);
        Task<List<Unit>> GetLinkedUnitsAsync(int unitId);
        Task<bool> AreUnitsLinkedAsync(int sourceUnitId, int targetUnitId);

        // Communication permissions
        Task<List<UnitCommunicationPermission>> GetCommunicationPermissionsAsync(int unitId);
        Task<UnitCommunicationPermission> CreateCommunicationPermissionAsync(UnitCommunicationPermission permission);
        Task<bool> RemoveCommunicationPermissionAsync(int permissionId);
        Task<bool> CanCommunicateAsync(int sourceUnitId, int targetUnitId, CommunicationType communicationType);
        Task<List<Unit>> GetAllowedCommunicationUnitsAsync(int unitId, CommunicationType communicationType);

        // User communication checks
        Task<bool> CanUsersCommunicateAsync(int sourceUserId, int targetUserId, CommunicationType communicationType);
        Task<List<User>> GetCommunicableUsersAsync(int userId, CommunicationType communicationType);

        // Statistics and reporting
        Task<int> GetUnitUserCountAsync(int unitId);
        Task<int> GetTotalUnitsCountAsync();
        Task<Dictionary<int, int>> GetUnitsUserCountAsync();
        Task<List<Unit>> SearchUnitsAsync(string searchTerm);
    }
}
