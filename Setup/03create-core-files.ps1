# PowerShell Script to Create Core Domain Files
# Step 3: Create all core domain models, entities, and interfaces

param(
    [string]$SolutionName = "CloudMigrationTool"
)

Write-Host "Creating Core Domain Files..." -ForegroundColor Green

# Ensure we're in the solution directory
if (!(Test-Path "$SolutionName.sln")) {
    Write-Error "Solution file not found. Please run previous setup scripts first."
    exit 1
}

# Core Enums
Write-Host "Creating Enums..." -ForegroundColor Cyan

# MigrationType enum
@"
namespace CloudMigrationTool.Core.Enums
{
    public enum MigrationType
    {
        ActiveDirectory = 1,
        Exchange = 2,
        OneDrive = 3,
        SharePoint = 4
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Enums\MigrationType.cs" -Encoding UTF8

# MigrationStatus enum
@"
namespace CloudMigrationTool.Core.Enums
{
    public enum MigrationStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Paused = 4,
        Cancelled = 5
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Enums\MigrationStatus.cs" -Encoding UTF8

# ConnectionStatus enum
@"
namespace CloudMigrationTool.Core.Enums
{
    public enum ConnectionStatus
    {
        NotConfigured = 0,
        Connected = 1,
        Disconnected = 2,
        Error = 3
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Enums\ConnectionStatus.cs" -Encoding UTF8

# Core Entities
Write-Host "Creating Entities..." -ForegroundColor Cyan

# Base Entity
@"
using System.ComponentModel.DataAnnotations;

namespace CloudMigrationTool.Core.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Entities\BaseEntity.cs" -Encoding UTF8

# ConnectionSettings Entity
@"
using System.ComponentModel.DataAnnotations;
using CloudMigrationTool.Core.Enums;

namespace CloudMigrationTool.Core.Entities
{
    public class ConnectionSettings : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public MigrationType Type { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public string ConnectionString { get; set; } = string.Empty;
        
        public string? AdditionalSettings { get; set; }
        
        public ConnectionStatus Status { get; set; } = ConnectionStatus.NotConfigured;
        
        public DateTime? LastTestedAt { get; set; }
        
        public string? LastTestResult { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<MigrationJob> MigrationJobs { get; set; } = new List<MigrationJob>();
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Entities\ConnectionSettings.cs" -Encoding UTF8

# MigrationJob Entity
@"
using System.ComponentModel.DataAnnotations;
using CloudMigrationTool.Core.Enums;

namespace CloudMigrationTool.Core.Entities
{
    public class MigrationJob : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public MigrationType Type { get; set; }
        
        public MigrationStatus Status { get; set; } = MigrationStatus.NotStarted;
        
        public int SourceConnectionId { get; set; }
        public virtual ConnectionSettings SourceConnection { get; set; } = null!;
        
        public int DestinationConnectionId { get; set; }
        public virtual ConnectionSettings DestinationConnection { get; set; } = null!;
        
        public string? Configuration { get; set; }
        
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public int TotalItems { get; set; } = 0;
        public int ProcessedItems { get; set; } = 0;
        public int SuccessfulItems { get; set; } = 0;
        public int FailedItems { get; set; } = 0;
        
        public string? ErrorMessage { get; set; }
        public string? LogFilePath { get; set; }
        
        // Navigation properties
        public virtual ICollection<MigrationJobLog> Logs { get; set; } = new List<MigrationJobLog>();
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Entities\MigrationJob.cs" -Encoding UTF8

# MigrationJobLog Entity
@"
using System.ComponentModel.DataAnnotations;

namespace CloudMigrationTool.Core.Entities
{
    public class MigrationJobLog : BaseEntity
    {
        [Required]
        public int MigrationJobId { get; set; }
        public virtual MigrationJob MigrationJob { get; set; } = null!;
        
        [Required]
        [MaxLength(50)]
        public string Level { get; set; } = string.Empty; // Info, Warning, Error
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? Details { get; set; }
        
        public string? Source { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Entities\MigrationJobLog.cs" -Encoding UTF8

# AppSettings Entity
@"
using System.ComponentModel.DataAnnotations;

namespace CloudMigrationTool.Core.Entities
{
    public class AppSettings : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(50)]
        public string? Category { get; set; }
        
        public bool IsEncrypted { get; set; } = false;
        
        public bool IsReadOnly { get; set; } = false;
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Entities\AppSettings.cs" -Encoding UTF8

# Core Models
Write-Host "Creating Models..." -ForegroundColor Cyan

# Connection Test Result Model
@"
using CloudMigrationTool.Core.Enums;

namespace CloudMigrationTool.Core.Models
{
    public class ConnectionTestResult
    {
        public bool IsSuccessful { get; set; }
        public string? Message { get; set; }
        public ConnectionStatus Status { get; set; }
        public DateTime TestTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string>? AdditionalInfo { get; set; }
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Models\ConnectionTestResult.cs" -Encoding UTF8

# Migration Progress Model
@"
namespace CloudMigrationTool.Core.Models
{
    public class MigrationProgress
    {
        public int JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public double PercentageComplete => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
        public DateTime? StartTime { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public string? CurrentOperation { get; set; }
        public string? LastError { get; set; }
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Models\MigrationProgress.cs" -Encoding UTF8

# Active Directory User Model
@"
namespace CloudMigrationTool.Core.Models
{
    public class AdUser
    {
        public string? SamAccountName { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? DisplayName { get; set; }
        public string? GivenName { get; set; }
        public string? Surname { get; set; }
        public string? EmailAddress { get; set; }
        public string? Department { get; set; }
        public string? Title { get; set; }
        public string? Manager { get; set; }
        public bool Enabled { get; set; }
        public DateTime? LastLogon { get; set; }
        public DateTime? PasswordLastSet { get; set; }
        public string? DistinguishedName { get; set; }
        public List<string> MemberOf { get; set; } = new List<string>();
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Models\AdUser.cs" -Encoding UTF8

# Exchange Mailbox Model
@"
namespace CloudMigrationTool.Core.Models
{
    public class ExchangeMailbox
    {
        public string? PrimarySmtpAddress { get; set; }
        public string? DisplayName { get; set; }
        public string? Alias { get; set; }
        public string? SamAccountName { get; set; }
        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }
        public long TotalItemSize { get; set; }
        public int ItemCount { get; set; }
        public DateTime? LastLogonTime { get; set; }
        public bool IsArchiveEnabled { get; set; }
        public string? MailboxType { get; set; }
        public List<string> EmailAddresses { get; set; } = new List<string>();
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Models\ExchangeMailbox.cs" -Encoding UTF8

Write-Host "Creating Interfaces..." -ForegroundColor Cyan

# Generic Repository Interface
@"
using CloudMigrationTool.Core.Entities;
using System.Linq.Expressions;

namespace CloudMigrationTool.Core.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Interfaces\IRepository.cs" -Encoding UTF8

# Unit of Work Interface
@"
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
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Interfaces\IUnitOfWork.cs" -Encoding UTF8

# Service Interfaces
@"
using CloudMigrationTool.Core.Models;

namespace CloudMigrationTool.Core.Interfaces
{
    public interface IActiveDirectoryService
    {
        Task<ConnectionTestResult> TestConnectionAsync(string connectionString);
        Task<IEnumerable<AdUser>> GetUsersAsync(string? searchFilter = null);
        Task<AdUser?> GetUserAsync(string userPrincipalName);
        Task<IEnumerable<string>> GetGroupsAsync();
        Task<IEnumerable<AdUser>> GetGroupMembersAsync(string groupName);
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Interfaces\IActiveDirectoryService.cs" -Encoding UTF8

@"
using CloudMigrationTool.Core.Models;

namespace CloudMigrationTool.Core.Interfaces
{
    public interface IExchangeService
    {
        Task<ConnectionTestResult> TestConnectionAsync(string connectionString);
        Task<IEnumerable<ExchangeMailbox>> GetMailboxesAsync();
        Task<ExchangeMailbox?> GetMailboxAsync(string emailAddress);
        Task<bool> MigrateMailboxAsync(string sourceMailbox, string destinationMailbox, IProgress<MigrationProgress>? progress = null);
    }
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Interfaces\IExchangeService.cs" -Encoding UTF8

@"
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
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Interfaces\IMigrationService.cs" -Encoding UTF8

@"
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
"@ | Out-File -FilePath "src\CloudMigrationTool.Core\Interfaces\IConfigurationService.cs" -Encoding UTF8

# Remove default Class1.cs
Remove-Item "src\CloudMigrationTool.Core\Class1.cs" -ErrorAction SilentlyContinue

Write-Host "`nCore domain files created successfully!" -ForegroundColor Green
Write-Host "Next: Run 04-Create-Infrastructure-Files.ps1 to create the infrastructure layer" -ForegroundColor Yellow