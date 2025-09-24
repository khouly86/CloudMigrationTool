using CloudMigrationTool.Core.Entities;

namespace CloudMigrationTool.Services.Admin
{
    public interface IConnectionSettingsService
    {
        Task<IEnumerable<ConnectionSettings>> GetAllAsync();
        Task<ConnectionSettings?> GetByIdAsync(int id);
        Task<ConnectionSettings> CreateAsync(ConnectionSettings connectionSettings);
        Task<ConnectionSettings> UpdateAsync(ConnectionSettings connectionSettings);
        Task<bool> DeleteAsync(int id);
        Task<bool> TestConnectionAsync(int id);
    }
}