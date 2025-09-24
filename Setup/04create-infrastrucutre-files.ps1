# PowerShell Script to Create Infrastructure Files
# Step 4: Create infrastructure layer (Data access, External services)

param(
    [string]$SolutionName = "CloudMigrationTool"
)

Write-Host "Creating Infrastructure Files..." -ForegroundColor Green

# Ensure we're in the solution directory
if (!(Test-Path "$SolutionName.sln")) {
    Write-Error "Solution file not found. Please run previous setup scripts first."
    exit 1
}

Write-Host "Creating Data Context..." -ForegroundColor Cyan

# ApplicationDbContext
@"
using CloudMigrationTool.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudMigrationTool.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ConnectionSettings> ConnectionSettings { get; set; } = null!;
        public DbSet<MigrationJob> MigrationJobs { get; set; } = null!;
        public DbSet<MigrationJobLog> MigrationJobLogs { get; set; } = null!;
        public DbSet<AppSettings> AppSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ConnectionSettings configuration
            modelBuilder.Entity<ConnectionSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ConnectionString).IsRequired();
                
                entity.HasMany(e => e.MigrationJobs)
                      .WithOne()
                      .HasForeignKey(j => j.SourceConnectionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // MigrationJob configuration
            modelBuilder.Entity<MigrationJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                
                entity.HasOne(e => e.SourceConnection)
                      .WithMany()
                      .HasForeignKey(e => e.SourceConnectionId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(e => e.DestinationConnection)
                      .WithMany()
                      .HasForeignKey(e => e.DestinationConnectionId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasMany(e => e.Logs)
                      .WithOne(l => l.MigrationJob)
                      .HasForeignKey(l => l.MigrationJobId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // MigrationJobLog configuration
            modelBuilder.Entity<MigrationJobLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Level).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.HasIndex(e => new { e.MigrationJobId, e.Timestamp });
            });

            // AppSettings configuration
            modelBuilder.Entity<AppSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Key).IsUnique();
                entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(50);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default app settings
            modelBuilder.Entity<AppSettings>().HasData(
                new AppSettings
                {
                    Id = 1,
                    Key = "DefaultLogLevel",
                    Value = "Information",
                    Description = "Default logging level for the application",
                    Category = "Logging",
                    CreatedAt = DateTime.UtcNow
                },
                new AppSettings
                {
                    Id = 2,
                    Key = "MaxConcurrentMigrations",
                    Value = "3",
                    Description = "Maximum number of concurrent migration jobs",
                    Category = "Migration",
                    CreatedAt = DateTime.UtcNow
                },
                new AppSettings
                {
                    Id = 3,
                    Key = "MigrationTimeoutMinutes",
                    Value = "480",
                    Description = "Migration timeout in minutes (8 hours)",
                    Category = "Migration",
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Infrastructure\Data\ApplicationDbContext.cs" -Encoding UTF8

Write-Host "Creating Repositories..." -ForegroundColor Cyan

# Generic Repository Implementation
@"
using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CloudMigrationTool.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.Where(e => !e.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).Where(e => !e.IsDeleted).ToListAsync();
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).FirstOrDefaultAsync(e => !e.IsDeleted);
        }

        public async Task<T> AddAsync(T entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
            return await Task.FromResult(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                await UpdateAsync(entity);
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.AnyAsync(e => e.Id == id && !e.IsDeleted);
        }

        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync(e => !e.IsDeleted);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).CountAsync(e => !e.IsDeleted);
        }
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Infrastructure\Repositories\Repository.cs" -Encoding UTF8

# Unit of Work Implementation
@"
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
"@ | Out-File -FilePath "src\CloudMigrationTool.Infrastructure\Repositories\UnitOfWork.cs" -Encoding UTF8

Write-Host "Creating External Service Implementations..." -ForegroundColor Cyan

# Active Directory Service Implementation
@"
using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Enums;
using Microsoft.Extensions.Logging;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace CloudMigrationTool.Infrastructure.External
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly ILogger<ActiveDirectoryService> _logger;

        public ActiveDirectoryService(ILogger<ActiveDirectoryService> logger)
        {
            _logger = logger;
        }

        public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString)
        {
            try
            {
                // Parse connection string to get domain info
                var connectionParams = ParseConnectionString(connectionString);
                
                using var context = new PrincipalContext(
                    ContextType.Domain,
                    connectionParams.GetValueOrDefault("Domain"),
                    connectionParams.GetValueOrDefault("Container"),
                    connectionParams.GetValueOrDefault("Username"),
                    connectionParams.GetValueOrDefault("Password")
                );

                // Test connection by attempting to find a user
                using var searcher = new PrincipalSearcher(new UserPrincipal(context));
                var result = searcher.FindOne();

                return new ConnectionTestResult
                {
                    IsSuccessful = true,
                    Status = ConnectionStatus.Connected,
                    Message = "Successfully connected to Active Directory",
                    TestTime = DateTime.UtcNow,
                    AdditionalInfo = new Dictionary<string, string>
                    {
                        { "Domain", connectionParams.GetValueOrDefault("Domain", "Unknown") },
                        { "TestUser", result?.SamAccountName ?? "No users found" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test Active Directory connection");
                return new ConnectionTestResult
                {
                    IsSuccessful = false,
                    Status = ConnectionStatus.Error,
                    Message = $"Connection failed: {ex.Message}",
                    TestTime = DateTime.UtcNow
                };
            }
        }

        public async Task<IEnumerable<AdUser>> GetUsersAsync(string? searchFilter = null)
        {
            var users = new List<AdUser>();
            
            try
            {
                // This would use the configured connection string in a real implementation
                // For now, using current domain context
                using var context = new PrincipalContext(ContextType.Domain);
                using var searcher = new PrincipalSearcher(new UserPrincipal(context));
                
                foreach (var result in searcher.FindAll())
                {
                    if (result is UserPrincipal userPrincipal)
                    {
                        var user = new AdUser
                        {
                            SamAccountName = userPrincipal.SamAccountName,
                            UserPrincipalName = userPrincipal.UserPrincipalName,
                            DisplayName = userPrincipal.DisplayName,
                            GivenName = userPrincipal.GivenName,
                            Surname = userPrincipal.Surname,
                            EmailAddress = userPrincipal.EmailAddress,
                            Enabled = userPrincipal.Enabled ?? false,
                            LastLogon = userPrincipal.LastLogon,
                            DistinguishedName = userPrincipal.DistinguishedName
                        };
                        
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Active Directory users");
            }

            return users;
        }

        public async Task<AdUser?> GetUserAsync(string userPrincipalName)
        {
            try
            {
                using var context = new PrincipalContext(ContextType.Domain);
                using var userPrincipal = UserPrincipal.FindByIdentity(context, userPrincipalName);
                
                if (userPrincipal == null) return null;

                return new AdUser
                {
                    SamAccountName = userPrincipal.SamAccountName,
                    UserPrincipalName = userPrincipal.UserPrincipalName,
                    DisplayName = userPrincipal.DisplayName,
                    GivenName = userPrincipal.GivenName,
                    Surname = userPrincipal.Surname,
                    EmailAddress = userPrincipal.EmailAddress,
                    Enabled = userPrincipal.Enabled ?? false,
                    LastLogon = userPrincipal.LastLogon,
                    DistinguishedName = userPrincipal.DistinguishedName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user {UserPrincipalName}", userPrincipalName);
                return null;
            }
        }

        public async Task<IEnumerable<string>> GetGroupsAsync()
        {
            var groups = new List<string>();
            
            try
            {
                using var context = new PrincipalContext(ContextType.Domain);
                using var searcher = new PrincipalSearcher(new GroupPrincipal(context));
                
                foreach (var result in searcher.FindAll())
                {
                    if (result is GroupPrincipal groupPrincipal)
                    {
                        groups.Add(groupPrincipal.Name ?? string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Active Directory groups");
            }

            return groups;
        }

        public async Task<IEnumerable<AdUser>> GetGroupMembersAsync(string groupName)
        {
            var users = new List<AdUser>();
            
            try
            {
                using var context = new PrincipalContext(ContextType.Domain);
                using var group = GroupPrincipal.FindByIdentity(context, groupName);
                
                if (group != null)
                {
                    foreach (var member in group.Members)
                    {
                        if (member is UserPrincipal userPrincipal)
                        {
                            var user = new AdUser
                            {
                                SamAccountName = userPrincipal.SamAccountName,
                                UserPrincipalName = userPrincipal.UserPrincipalName,
                                DisplayName = userPrincipal.DisplayName,
                                GivenName = userPrincipal.GivenName,
                                Surname = userPrincipal.Surname,
                                EmailAddress = userPrincipal.EmailAddress,
                                Enabled = userPrincipal.Enabled ?? false,
                                DistinguishedName = userPrincipal.DistinguishedName
                            };
                            
                            users.Add(user);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve members for group {GroupName}", groupName);
            }

            return users;
        }

        private Dictionary<string, string> ParseConnectionString(string connectionString)
        {
            var parameters = new Dictionary<string, string>();
            
            foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
            
            return parameters;
        }
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Infrastructure\External\ActiveDirectoryService.cs" -Encoding UTF8

# Exchange Service Implementation
@"
using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Exchange.WebServices.Data;

namespace CloudMigrationTool.Infrastructure.External
{
    public class ExchangeService : IExchangeService
    {
        private readonly ILogger<ExchangeService> _logger;

        public ExchangeService(ILogger<ExchangeService> logger)
        {
            _logger = logger;
        }

        public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString)
        {
            try
            {
                var connectionParams = ParseConnectionString(connectionString);
                var service = CreateExchangeService(connectionParams);
                
                // Test connection by getting server info
                var response = await Task.Run(() => 
                {
                    service.ResolveName("test", ResolveNameSearchLocation.DirectoryOnly, false);
                    return true;
                });

                return new ConnectionTestResult
                {
                    IsSuccessful = true,
                    Status = ConnectionStatus.Connected,
                    Message = "Successfully connected to Exchange Server",
                    TestTime = DateTime.UtcNow,
                    AdditionalInfo = new Dictionary<string, string>
                    {
                        { "ServerUrl", connectionParams.GetValueOrDefault("ServerUrl", "Unknown") },
                        { "Version", service.ServerInfo?.MajorVersion.ToString() ?? "Unknown" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test Exchange connection");
                return new ConnectionTestResult
                {
                    IsSuccessful = false,
                    Status = ConnectionStatus.Error,
                    Message = $"Connection failed: {ex.Message}",
                    TestTime = DateTime.UtcNow
                };
            }
        }

        public async Task<IEnumerable<ExchangeMailbox>> GetMailboxesAsync()
        {
            var mailboxes = new List<ExchangeMailbox>();
            
            try
            {
                // In a real implementation, this would connect to Exchange Management Shell
                // or use Exchange Web Services to enumerate mailboxes
                _logger.LogInformation("Retrieving Exchange mailboxes...");
                
                // Placeholder implementation
                await Task.Delay(100); // Simulate async operation
                
                // This would be replaced with actual Exchange PowerShell commands or EWS calls
                mailboxes.Add(new ExchangeMailbox
                {
                    PrimarySmtpAddress = "user1@company.com",
                    DisplayName = "User One",
                    Alias = "user1",
                    SamAccountName = "user1",
                    ServerName = "EXCHANGE01",
                    DatabaseName = "Mailbox Database 01",
                    TotalItemSize = 1024 * 1024 * 100, // 100MB
                    ItemCount = 1500,
                    LastLogonTime = DateTime.Now.AddDays(-1),
                    IsArchiveEnabled = false,
                    MailboxType = "UserMailbox"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Exchange mailboxes");
            }

            return mailboxes;
        }

        public async Task<ExchangeMailbox?> GetMailboxAsync(string emailAddress)
        {
            try
            {
                // In a real implementation, this would query Exchange for the specific mailbox
                var mailboxes = await GetMailboxesAsync();
                return mailboxes.FirstOrDefault(m => 
                    m.PrimarySmtpAddress?.Equals(emailAddress, StringComparison.OrdinalIgnoreCase) == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve mailbox {EmailAddress}", emailAddress);
                return null;
            }
        }

        public async Task<bool> MigrateMailboxAsync(string sourceMailbox, string destinationMailbox, 
            IProgress<MigrationProgress>? progress = null)
        {
            try
            {
                _logger.LogInformation("Starting mailbox migration from {Source} to {Destination}", 
                    sourceMailbox, destinationMailbox);

                // Simulate migration progress
                var totalItems = 1000;
                var progressInfo = new MigrationProgress
                {
                    JobId = 0,
                    JobName = $"Migrate {sourceMailbox}",
                    TotalItems = totalItems,
                    StartTime = DateTime.UtcNow
                };

                for (int i = 0; i <= totalItems; i += 50)
                {
                    await Task.Delay(1000); // Simulate work
                    
                    progressInfo.ProcessedItems = i;
                    progressInfo.SuccessfulItems = i - (i / 100); // 99% success rate
                    progressInfo.FailedItems = i / 100;
                    progressInfo.CurrentOperation = $"Processing items {i}-{Math.Min(i + 50, totalItems)}";
                    
                    progress?.Report(progressInfo);
                }

                _logger.LogInformation("Completed mailbox migration from {Source} to {Destination}", 
                    sourceMailbox, destinationMailbox);
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate mailbox from {Source} to {Destination}", 
                    sourceMailbox, destinationMailbox);
                return false;
            }
        }

        private Microsoft.Exchange.WebServices.Data.ExchangeService CreateExchangeService(
            Dictionary<string, string> connectionParams)
        {
            var service = new Microsoft.Exchange.WebServices.Data.ExchangeService();
            
            if (connectionParams.TryGetValue("ServerUrl", out var serverUrl))
            {
                service.Url = new Uri(serverUrl);
            }
            
            if (connectionParams.TryGetValue("Username", out var username) && 
                connectionParams.TryGetValue("Password", out var password))
            {
                service.Credentials = new WebCredentials(username, password);
            }
            else
            {
                service.UseDefaultCredentials = true;
            }
            
            return service;
        }

        private Dictionary<string, string> ParseConnectionString(string connectionString)
        {
            var parameters = new Dictionary<string, string>();
            
            foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
            
            return parameters;
        }
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Infrastructure\External\ExchangeService.cs" -Encoding UTF8

# Remove default Class1.cs
Remove-Item "src\CloudMigrationTool.Infrastructure\Class1.cs" -ErrorAction SilentlyContinue

Write-Host "`nInfrastructure files created successfully!" -ForegroundColor Green
Write-Host "Next: Run 05-Create-Services-Files.ps1 to create the service layer" -ForegroundColor Yellow