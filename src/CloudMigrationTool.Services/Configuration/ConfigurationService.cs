using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CloudMigrationTool.Services.Configuration
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ConfigurationService> _logger;

        public ConfigurationService(IUnitOfWork unitOfWork, ILogger<ConfigurationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            try
            {
                var setting = await _unitOfWork.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
                return setting?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get setting {Key}", key);
                return null;
            }
        }

        public async Task<T?> GetSettingAsync<T>(string key)
        {
            try
            {
                var value = await GetSettingAsync(key);
                if (string.IsNullOrEmpty(value)) return default(T);

                if (typeof(T) == typeof(string)) return (T)(object)value;
                if (typeof(T) == typeof(int)) return (T)(object)int.Parse(value);
                if (typeof(T) == typeof(bool)) return (T)(object)bool.Parse(value);
                if (typeof(T) == typeof(DateTime)) return (T)(object)DateTime.Parse(value);

                // For complex types, try JSON deserialization
                return JsonSerializer.Deserialize<T>(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get setting {Key} as type {Type}", key, typeof(T).Name);
                return default(T);
            }
        }

        public async Task SetSettingAsync(string key, string value, string? description = null, string? category = null)
        {
            try
            {
                var existingSetting = await _unitOfWork.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
                
                if (existingSetting != null)
                {
                    existingSetting.Value = value;
                    existingSetting.Description = description ?? existingSetting.Description;
                    existingSetting.Category = category ?? existingSetting.Category;
                    await _unitOfWork.AppSettings.UpdateAsync(existingSetting);
                }
                else
                {
                    var newSetting = new AppSettings
                    {
                        Key = key,
                        Value = value,
                        Description = description,
                        Category = category
                    };
                    await _unitOfWork.AppSettings.AddAsync(newSetting);
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Setting {Key} updated successfully", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set setting {Key}", key);
                throw;
            }
        }

        public async Task SetSettingAsync<T>(string key, T value, string? description = null, string? category = null)
        {
            try
            {
                string serializedValue;
                
                if (value is string stringValue)
                    serializedValue = stringValue;
                else if (value is int || value is bool || value is DateTime)
                    serializedValue = value.ToString() ?? string.Empty;
                else
                    serializedValue = JsonSerializer.Serialize(value);

                await SetSettingAsync(key, serializedValue, description, category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set setting {Key} with type {Type}", key, typeof(T).Name);
                throw;
            }
        }

        public async Task<bool> DeleteSettingAsync(string key)
        {
            try
            {
                var setting = await _unitOfWork.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
                if (setting == null) return false;

                if (setting.IsReadOnly)
                {
                    _logger.LogWarning("Attempted to delete read-only setting {Key}", key);
                    return false;
                }

                await _unitOfWork.AppSettings.DeleteAsync(setting.Id);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Setting {Key} deleted successfully", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete setting {Key}", key);
                return false;
            }
        }

        public async Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category)
        {
            try
            {
                var settings = await _unitOfWork.AppSettings.FindAsync(s => s.Category == category);
                return settings.ToDictionary(s => s.Key, s => s.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get settings for category {Category}", category);
                return new Dictionary<string, string>();
            }
        }

        public async Task<ConnectionTestResult> TestConnectionSettingAsync(string connectionName)
        {
            try
            {
                var connection = await _unitOfWork.ConnectionSettings.FirstOrDefaultAsync(c => c.Name == connectionName);
                if (connection == null)
                {
                    return new ConnectionTestResult
                    {
                        IsSuccessful = false,
                        Status = ConnectionStatus.NotConfigured,
                        Message = $"Connection '{connectionName}' not found"
                    };
                }

                // This would delegate to the appropriate service based on connection type
                // For now, returning a basic result
                return new ConnectionTestResult
                {
                    IsSuccessful = true,
                    Status = ConnectionStatus.Connected,
                    Message = $"Connection '{connectionName}' test would be performed here"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test connection {ConnectionName}", connectionName);
                return new ConnectionTestResult
                {
                    IsSuccessful = false,
                    Status = ConnectionStatus.Error,
                    Message = ex.Message
                };
            }
        }
    }
}
