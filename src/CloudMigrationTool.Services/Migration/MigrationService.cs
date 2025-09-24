using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Enums;
using Microsoft.Extensions.Logging;

namespace CloudMigrationTool.Services.Migration
{
    public class MigrationService : IMigrationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MigrationService> _logger;
        private readonly IActiveDirectoryService _activeDirectoryService;
        private readonly IExchangeService _exchangeService;

        public MigrationService(
            IUnitOfWork unitOfWork,
            ILogger<MigrationService> logger,
            IActiveDirectoryService activeDirectoryService,
            IExchangeService exchangeService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _activeDirectoryService = activeDirectoryService;
            _exchangeService = exchangeService;
        }

        public async Task<int> CreateMigrationJobAsync(MigrationJob migrationJob)
        {
            try
            {
                // Validate connections exist
                var sourceConnection = await _unitOfWork.ConnectionSettings.GetByIdAsync(migrationJob.SourceConnectionId);
                var destinationConnection = await _unitOfWork.ConnectionSettings.GetByIdAsync(migrationJob.DestinationConnectionId);

                if (sourceConnection == null || destinationConnection == null)
                {
                    throw new ArgumentException("Source or destination connection not found");
                }

                // Set initial status and metadata
                migrationJob.Status = MigrationStatus.NotStarted;
                migrationJob.CreatedAt = DateTime.UtcNow;

                // Add the job
                await _unitOfWork.MigrationJobs.AddAsync(migrationJob);
                await _unitOfWork.SaveChangesAsync();

                // Log the creation
                await LogMigrationEventAsync(migrationJob.Id, "Info", "Migration job created", 
                    $"Job: {migrationJob.Name}, Type: {migrationJob.Type}");

                _logger.LogInformation("Created migration job {JobId} - {JobName}", migrationJob.Id, migrationJob.Name);
                return migrationJob.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create migration job {JobName}", migrationJob.Name);
                throw;
            }
        }

        public async Task<bool> StartMigrationJobAsync(int jobId)
        {
            try
            {
                var job = await _unitOfWork.MigrationJobs.GetByIdAsync(jobId);
                if (job == null)
                {
                    _logger.LogWarning("Migration job {JobId} not found", jobId);
                    return false;
                }

                if (job.Status != MigrationStatus.NotStarted && job.Status != MigrationStatus.Paused)
                {
                    _logger.LogWarning("Cannot start job {JobId} with status {Status}", jobId, job.Status);
                    return false;
                }

                // Update job status
                job.Status = MigrationStatus.InProgress;
                job.StartedAt = DateTime.UtcNow;
                await _unitOfWork.MigrationJobs.UpdateAsync(job);
                await _unitOfWork.SaveChangesAsync();

                // Log the start
                await LogMigrationEventAsync(jobId, "Info", "Migration job started");

                // Start the actual migration process (fire and forget)
                _ = Task.Run(() => ExecuteMigrationJobAsync(jobId));

                _logger.LogInformation("Started migration job {JobId}", jobId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start migration job {JobId}", jobId);
                return false;
            }
        }

        public async Task<bool> PauseMigrationJobAsync(int jobId)
        {
            try
            {
                var job = await _unitOfWork.MigrationJobs.GetByIdAsync(jobId);
                if (job == null || job.Status != MigrationStatus.InProgress)
                {
                    return false;
                }

                job.Status = MigrationStatus.Paused;
                await _unitOfWork.MigrationJobs.UpdateAsync(job);
                await _unitOfWork.SaveChangesAsync();

                await LogMigrationEventAsync(jobId, "Info", "Migration job paused");
                _logger.LogInformation("Paused migration job {JobId}", jobId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pause migration job {JobId}", jobId);
                return false;
            }
        }

        public async Task<bool> CancelMigrationJobAsync(int jobId)
        {
            try
            {
                var job = await _unitOfWork.MigrationJobs.GetByIdAsync(jobId);
                if (job == null)
                {
                    return false;
                }

                job.Status = MigrationStatus.Cancelled;
                job.CompletedAt = DateTime.UtcNow;
                await _unitOfWork.MigrationJobs.UpdateAsync(job);
                await _unitOfWork.SaveChangesAsync();

                await LogMigrationEventAsync(jobId, "Warning", "Migration job cancelled");
                _logger.LogInformation("Cancelled migration job {JobId}", jobId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel migration job {JobId}", jobId);
                return false;
            }
        }

        public async Task<MigrationProgress?> GetMigrationProgressAsync(int jobId)
        {
            try
            {
                var job = await _unitOfWork.MigrationJobs.GetByIdAsync(jobId);
                if (job == null) return null;

                return new MigrationProgress
                {
                    JobId = job.Id,
                    JobName = job.Name,
                    TotalItems = job.TotalItems,
                    ProcessedItems = job.ProcessedItems,
                    SuccessfulItems = job.SuccessfulItems,
                    FailedItems = job.FailedItems,
                    StartTime = job.StartedAt,
                    CurrentOperation = GetCurrentOperation(job),
                    LastError = job.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get migration progress for job {JobId}", jobId);
                return null;
            }
        }

        public async Task<IEnumerable<MigrationJob>> GetActiveMigrationJobsAsync()
        {
            try
            {
                return await _unitOfWork.MigrationJobs.FindAsync(j => 
                    j.Status == MigrationStatus.InProgress || 
                    j.Status == MigrationStatus.Paused);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active migration jobs");
                return new List<MigrationJob>();
            }
        }

        public async Task LogMigrationEventAsync(int jobId, string level, string message, string? details = null)
        {
            try
            {
                var logEntry = new MigrationJobLog
                {
                    MigrationJobId = jobId,
                    Level = level,
                    Message = message,
                    Details = details,
                    Timestamp = DateTime.UtcNow,
                    Source = nameof(MigrationService)
                };

                await _unitOfWork.MigrationJobLogs.AddAsync(logEntry);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log migration event for job {JobId}", jobId);
            }
        }

        private async Task ExecuteMigrationJobAsync(int jobId)
        {
            try
            {
                var job = await _unitOfWork.MigrationJobs.GetByIdAsync(jobId);
                if (job == null) return;

                await LogMigrationEventAsync(jobId, "Info", "Starting migration execution");

                // Execute based on migration type
                bool success = job.Type switch
                {
                    MigrationType.ActiveDirectory => await ExecuteActiveDirectoryMigrationAsync(job),
                    MigrationType.Exchange => await ExecuteExchangeMigrationAsync(job),
                    _ => throw new NotImplementedException($"Migration type {job.Type} not implemented")
                };

                // Update final status
                job.Status = success ? MigrationStatus.Completed : MigrationStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;
                await _unitOfWork.MigrationJobs.UpdateAsync(job);
                await _unitOfWork.SaveChangesAsync();

                await LogMigrationEventAsync(jobId, success ? "Info" : "Error", 
                    success ? "Migration completed successfully" : "Migration failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing migration job {JobId}", jobId);
                
                var job = await _unitOfWork.MigrationJobs.GetByIdAsync(jobId);
                if (job != null)
                {
                    job.Status = MigrationStatus.Failed;
                    job.ErrorMessage = ex.Message;
                    job.CompletedAt = DateTime.UtcNow;
                    await _unitOfWork.MigrationJobs.UpdateAsync(job);
                    await _unitOfWork.SaveChangesAsync();

                    await LogMigrationEventAsync(jobId, "Error", "Migration failed with exception", ex.ToString());
                }
            }
        }

        private async Task<bool> ExecuteActiveDirectoryMigrationAsync(MigrationJob job)
        {
            try
            {
                await LogMigrationEventAsync(job.Id, "Info", "Starting Active Directory migration");
                
                // Get users from source AD
                var users = await _activeDirectoryService.GetUsersAsync();
                var userList = users.ToList();
                
                job.TotalItems = userList.Count;
                await _unitOfWork.MigrationJobs.UpdateAsync(job);
                await _unitOfWork.SaveChangesAsync();

                foreach (var user in userList)
                {
                    // Check if job is still active
                    var currentJob = await _unitOfWork.MigrationJobs.GetByIdAsync(job.Id);
                    if (currentJob?.Status != MigrationStatus.InProgress) break;

                    try
                    {
                        // Simulate user migration (placeholder)
                        await Task.Delay(100);
                        
                        job.ProcessedItems++;
                        job.SuccessfulItems++;
                    }
                    catch (Exception ex)
                    {
                        job.ProcessedItems++;
                        job.FailedItems++;
                        await LogMigrationEventAsync(job.Id, "Error", 
                            $"Failed to migrate user {user.UserPrincipalName}", ex.Message);
                    }

                    // Update progress every 10 items
                    if (job.ProcessedItems % 10 == 0)
                    {
                        await _unitOfWork.MigrationJobs.UpdateAsync(job);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                return job.FailedItems == 0;
            }
            catch (Exception ex)
            {
                await LogMigrationEventAsync(job.Id, "Error", "Active Directory migration failed", ex.ToString());
                return false;
            }
        }

        private async Task<bool> ExecuteExchangeMigrationAsync(MigrationJob job)
        {
            try
            {
                await LogMigrationEventAsync(job.Id, "Info", "Starting Exchange migration");
                
                // Get mailboxes from source Exchange
                var mailboxes = await _exchangeService.GetMailboxesAsync();
                var mailboxList = mailboxes.ToList();
                
                job.TotalItems = mailboxList.Count;
                await _unitOfWork.MigrationJobs.UpdateAsync(job);
                await _unitOfWork.SaveChangesAsync();

                foreach (var mailbox in mailboxList)
                {
                    // Check if job is still active
                    var currentJob = await _unitOfWork.MigrationJobs.GetByIdAsync(job.Id);
                    if (currentJob?.Status != MigrationStatus.InProgress) break;

                    try
                    {
                        // Migrate mailbox with progress reporting
                        var progress = new Progress<MigrationProgress>(p =>
                        {
                            // Update current operation status
                        });

                        bool migrated = await _exchangeService.MigrateMailboxAsync(
                            mailbox.PrimarySmtpAddress!, 
                            mailbox.PrimarySmtpAddress!, 
                            progress);

                        job.ProcessedItems++;
                        if (migrated)
                            job.SuccessfulItems++;
                        else
                            job.FailedItems++;
                    }
                    catch (Exception ex)
                    {
                        job.ProcessedItems++;
                        job.FailedItems++;
                        await LogMigrationEventAsync(job.Id, "Error", 
                            $"Failed to migrate mailbox {mailbox.PrimarySmtpAddress}", ex.Message);
                    }

                    // Update progress
                    await _unitOfWork.MigrationJobs.UpdateAsync(job);
                    await _unitOfWork.SaveChangesAsync();
                }

                return job.FailedItems == 0;
            }
            catch (Exception ex)
            {
                await LogMigrationEventAsync(job.Id, "Error", "Exchange migration failed", ex.ToString());
                return false;
            }
        }

        private string GetCurrentOperation(MigrationJob job)
        {
            return job.Status switch
            {
                MigrationStatus.InProgress => $"Processing {job.ProcessedItems}/{job.TotalItems} items",
                MigrationStatus.Completed => "Migration completed",
                MigrationStatus.Failed => "Migration failed",
                MigrationStatus.Paused => "Migration paused",
                MigrationStatus.Cancelled => "Migration cancelled",
                _ => "Not started"
            };
        }
    }
}
