using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CloudMigrationTool.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;
        
        private IRepository<ConnectionSettings>? _connectionSettings;
        private IRepository<MigrationJob>? _migrationJobs;
        private IRepository<MigrationJobLog>? _migrationJobLogs;
        private IRepository<AppSettings>? _appSettings;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<ConnectionSettings> ConnectionSettings =>
            _connectionSettings ??= new Repository<ConnectionSettings>(_context);

        public IRepository<MigrationJob> MigrationJobs =>
            _migrationJobs ??= new Repository<MigrationJob>(_context);

        public IRepository<MigrationJobLog> MigrationJobLogs =>
            _migrationJobLogs ??= new Repository<MigrationJobLog>(_context);

        public IRepository<AppSettings> AppSettings =>
            _appSettings ??= new Repository<AppSettings>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
