using CloudMigrationTool.Core.Models;

namespace CloudMigrationTool.Core.Interfaces
{
    public interface IConfigurationService
    {
        Task<string?> GetSettingAsync(string key);
        Task<T?> GetSettingAsync<T>(string key);
        Task SetSettingAsync(string key, string value, string? description = null, string? category = null);
        Task SetSettingAsync<T>(string key, T value, string? description = null, string? category = null);
        Task<bool> DeleteSettingAsync(string key);
        Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category);
        Task<ConnectionTestResult> TestConnectionSettingAsync(string connectionName);
    }
}
