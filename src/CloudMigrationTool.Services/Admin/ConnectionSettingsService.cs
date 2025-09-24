using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloudMigrationTool.Services.Admin
{
    public class ConnectionSettingsService : IConnectionSettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConnectionSettingsService> _logger;

        public ConnectionSettingsService(ApplicationDbContext context, ILogger<ConnectionSettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ConnectionSettings>> GetAllAsync()
        {
            try
            {
                return await _context.ConnectionSettings
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving connection settings");
                throw;
            }
        }

        public async Task<ConnectionSettings?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.ConnectionSettings
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving connection settings with ID {Id}", id);
                throw;
            }
        }

        public async Task<ConnectionSettings> CreateAsync(ConnectionSettings connectionSettings)
        {
            try
            {
                connectionSettings.CreatedAt = DateTime.UtcNow;
                connectionSettings.UpdatedAt = DateTime.UtcNow;

                _context.ConnectionSettings.Add(connectionSettings);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created connection settings: {Name} (ID: {Id})",
                    connectionSettings.Name, connectionSettings.Id);

                return connectionSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating connection settings: {Name}", connectionSettings.Name);
                throw;
            }
        }

        public async Task<ConnectionSettings> UpdateAsync(ConnectionSettings connectionSettings)
        {
            try
            {
                var existing = await _context.ConnectionSettings
                    .FirstOrDefaultAsync(c => c.Id == connectionSettings.Id);

                if (existing == null)
                {
                    throw new ArgumentException($"Connection settings with ID {connectionSettings.Id} not found");
                }

                existing.Name = connectionSettings.Name;
                existing.Type = connectionSettings.Type;
                existing.Description = connectionSettings.Description;
                existing.ConnectionString = connectionSettings.ConnectionString;
                existing.IsActive = connectionSettings.IsActive;
                existing.Status = connectionSettings.Status;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated connection settings: {Name} (ID: {Id})",
                    existing.Name, existing.Id);

                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating connection settings with ID {Id}", connectionSettings.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var connectionSettings = await _context.ConnectionSettings
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (connectionSettings == null)
                {
                    return false;
                }

                // Check if connection is being used by migration jobs
                var hasJobs = await _context.MigrationJobs
                    .AnyAsync(j => j.SourceConnectionId == id || j.DestinationConnectionId == id);

                if (hasJobs)
                {
                    throw new InvalidOperationException(
                        "Cannot delete connection settings because it is being used by migration jobs");
                }

                _context.ConnectionSettings.Remove(connectionSettings);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted connection settings: {Name} (ID: {Id})",
                    connectionSettings.Name, connectionSettings.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting connection settings with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync(int id)
        {
            try
            {
                var connectionSettings = await GetByIdAsync(id);
                if (connectionSettings == null)
                {
                    return false;
                }

                _logger.LogInformation("Testing connection: {Name} (ID: {Id})",
                    connectionSettings.Name, connectionSettings.Id);

                // TODO: Implement actual connection testing based on connection type
                // For now, simulate a test based on connection string validity
                bool isValid = !string.IsNullOrWhiteSpace(connectionSettings.ConnectionString);

                // Update connection status
                connectionSettings.Status = isValid ?
                    Core.Enums.ConnectionStatus.Connected :
                    Core.Enums.ConnectionStatus.Error;
                connectionSettings.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection with ID {Id}", id);

                // Update status to error
                try
                {
                    var connectionSettings = await GetByIdAsync(id);
                    if (connectionSettings != null)
                    {
                        connectionSettings.Status = Core.Enums.ConnectionStatus.Error;
                        connectionSettings.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Error updating connection status after test failure");
                }

                return false;
            }
        }
    }
}