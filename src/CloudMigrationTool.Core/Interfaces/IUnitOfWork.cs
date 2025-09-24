using CloudMigrationTool.Core.Entities;

namespace CloudMigrationTool.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<ConnectionSettings> ConnectionSettings { get; }
        IRepository<MigrationJob> MigrationJobs { get; }
        IRepository<MigrationJobLog> MigrationJobLogs { get; }
        IRepository<AppSettings> AppSettings { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
