using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Models;

namespace CloudMigrationTool.Core.Interfaces
{
    public interface IMigrationService
    {
        Task<int> CreateMigrationJobAsync(MigrationJob migrationJob);
        Task<bool> StartMigrationJobAsync(int jobId);
        Task<bool> PauseMigrationJobAsync(int jobId);
        Task<bool> CancelMigrationJobAsync(int jobId);
        Task<MigrationProgress?> GetMigrationProgressAsync(int jobId);
        Task<IEnumerable<MigrationJob>> GetActiveMigrationJobsAsync();
        Task LogMigrationEventAsync(int jobId, string level, string message, string? details = null);
    }
}
