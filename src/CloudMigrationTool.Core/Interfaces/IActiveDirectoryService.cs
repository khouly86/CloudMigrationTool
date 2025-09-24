using CloudMigrationTool.Core.Models;

namespace CloudMigrationTool.Core.Interfaces
{
    public interface IActiveDirectoryService
    {
        Task<ConnectionTestResult> TestConnectionAsync(string connectionString);
        Task<IEnumerable<AdUser>> GetUsersAsync(string? searchFilter = null);
        Task<AdUser?> GetUserAsync(string userPrincipalName);
        Task<IEnumerable<AdGroup>> GetGroupsAsync();
        Task<IEnumerable<AdUser>> GetGroupMembersAsync(string groupName);
        Task<IEnumerable<AdOrganizationalUnit>> GetOrganizationalUnitsAsync();
        Task<AdGroup?> GetGroupAsync(string groupId);
        Task<AdOrganizationalUnit?> GetOrganizationalUnitAsync(string ouId);
    }
}
