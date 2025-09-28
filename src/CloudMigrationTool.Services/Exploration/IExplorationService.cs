using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Models;

namespace CloudMigrationTool.Services.Exploration
{
    public interface IExplorationService
    {
        Task<IEnumerable<AdUser>> GetUsersForConnectionAsync(ConnectionSettings connection, string? searchFilter = null);
        Task<AdUser?> GetUserForConnectionAsync(ConnectionSettings connection, string userPrincipalName);
        Task<IEnumerable<AdGroup>> GetGroupsForConnectionAsync(ConnectionSettings connection);
        Task<IEnumerable<AdOrganizationalUnit>> GetOrganizationalUnitsForConnectionAsync(ConnectionSettings connection);
        Task<IEnumerable<AdUser>> GetGroupMembersForConnectionAsync(ConnectionSettings connection, string groupName);
        Task<IEnumerable<ExchangeMailbox>> GetMailboxesForConnectionAsync(ConnectionSettings connection);
        Task<ExchangeMailbox?> GetMailboxForConnectionAsync(ConnectionSettings connection, string emailAddress);
        Task<IEnumerable<DistributionGroup>> GetDistributionGroupsForConnectionAsync(ConnectionSettings connection);
        Task<IEnumerable<SharedMailbox>> GetSharedMailboxesForConnectionAsync(ConnectionSettings connection);
        Task<ConnectionTestResult> TestConnectionAsync(ConnectionSettings connection);
    }
}