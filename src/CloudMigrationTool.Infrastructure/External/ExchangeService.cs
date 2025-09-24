using CloudMigrationTool.Core.Interfaces;
using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Azure.Identity;
using System.Text.Json;

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

                // For demo purposes, we'll simulate a successful connection test
                // In a real implementation, you would create a Graph client and test it
                await Task.Delay(100); // Simulate async work

                return new ConnectionTestResult
                {
                    IsSuccessful = true,
                    Status = ConnectionStatus.Connected,
                    Message = "Successfully connected to Exchange Online (simulated)",
                    TestTime = DateTime.UtcNow,
                    AdditionalInfo = new Dictionary<string, string>
                    {
                        { "ConnectionType", "Exchange Online" },
                        { "Status", "Simulated Connection" }
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
                _logger.LogInformation("Retrieving Exchange mailboxes...");

                // Simulate async work
                await Task.Delay(100);

                // Sample mailboxes for demonstration
                var sampleMailboxes = new[]
                {
                    new ExchangeMailbox
                    {
                        PrimarySmtpAddress = "john.doe@company.com",
                        DisplayName = "John Doe",
                        Alias = "john.doe",
                        SamAccountName = "john.doe",
                        ServerName = "Exchange Online",
                        DatabaseName = "Cloud Database",
                        TotalItemSize = 1024 * 1024 * 150, // 150MB
                        ItemCount = 2500,
                        LastLogonTime = DateTime.Now.AddHours(-2),
                        IsArchiveEnabled = false,
                        MailboxType = "UserMailbox",
                        EmailAddresses = new List<string> { "john.doe@company.com", "j.doe@company.com" }
                    },
                    new ExchangeMailbox
                    {
                        PrimarySmtpAddress = "jane.smith@company.com",
                        DisplayName = "Jane Smith",
                        Alias = "jane.smith",
                        SamAccountName = "jane.smith",
                        ServerName = "Exchange Online",
                        DatabaseName = "Cloud Database",
                        TotalItemSize = 1024 * 1024 * 320, // 320MB
                        ItemCount = 4800,
                        LastLogonTime = DateTime.Now.AddDays(-1),
                        IsArchiveEnabled = true,
                        MailboxType = "UserMailbox",
                        EmailAddresses = new List<string> { "jane.smith@company.com" }
                    },
                    new ExchangeMailbox
                    {
                        PrimarySmtpAddress = "shared@company.com",
                        DisplayName = "Shared Mailbox",
                        Alias = "shared",
                        SamAccountName = "shared",
                        ServerName = "Exchange Online",
                        DatabaseName = "Cloud Database",
                        TotalItemSize = 1024 * 1024 * 75, // 75MB
                        ItemCount = 890,
                        LastLogonTime = DateTime.Now.AddDays(-3),
                        IsArchiveEnabled = false,
                        MailboxType = "SharedMailbox",
                        EmailAddresses = new List<string> { "shared@company.com", "team@company.com" }
                    }
                };

                mailboxes.AddRange(sampleMailboxes);

                _logger.LogInformation("Retrieved {Count} mailboxes", mailboxes.Count);
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
                _logger.LogInformation("Retrieving mailbox for {EmailAddress}", emailAddress);

                var mailboxes = await GetMailboxesAsync();
                var mailbox = mailboxes.FirstOrDefault(m =>
                    m.PrimarySmtpAddress?.Equals(emailAddress, StringComparison.OrdinalIgnoreCase) == true);

                if (mailbox != null)
                {
                    _logger.LogInformation("Found mailbox for {EmailAddress}", emailAddress);
                }
                else
                {
                    _logger.LogWarning("Mailbox not found for {EmailAddress}", emailAddress);
                }

                return mailbox;
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

                // Get the source mailbox to determine total items
                var mailbox = await GetMailboxAsync(sourceMailbox);
                var totalItems = mailbox?.ItemCount ?? 1000;

                var progressInfo = new MigrationProgress
                {
                    JobId = 0,
                    JobName = $"Migrate {sourceMailbox}",
                    TotalItems = totalItems,
                    StartTime = DateTime.UtcNow
                };

                // Simulate migration with realistic progress
                var batchSize = Math.Max(1, totalItems / 20); // Process in 20 batches

                for (int i = 0; i <= totalItems; i += batchSize)
                {
                    // Simulate processing time
                    await Task.Delay(Random.Shared.Next(500, 1500));

                    var currentBatch = Math.Min(batchSize, totalItems - i);
                    progressInfo.ProcessedItems = Math.Min(i + currentBatch, totalItems);

                    // Simulate 97% success rate
                    var successfulInBatch = (int)(currentBatch * 0.97);
                    progressInfo.SuccessfulItems += successfulInBatch;
                    progressInfo.FailedItems += (currentBatch - successfulInBatch);

                    progressInfo.CurrentOperation = $"Processing items {i + 1}-{Math.Min(i + currentBatch, totalItems)} of {totalItems}";

                    // Calculate estimated time remaining
                    if (i > 0)
                    {
                        var elapsed = DateTime.UtcNow - progressInfo.StartTime!.Value;
                        var avgTimePerItem = elapsed.TotalMilliseconds / progressInfo.ProcessedItems;
                        var remainingItems = totalItems - progressInfo.ProcessedItems;
                        progressInfo.EstimatedTimeRemaining = TimeSpan.FromMilliseconds(avgTimePerItem * remainingItems);
                    }

                    progress?.Report(progressInfo);

                    _logger.LogDebug("Migration progress: {Processed}/{Total} items ({Percentage:F1}%)",
                        progressInfo.ProcessedItems, totalItems, progressInfo.PercentageComplete);
                }

                _logger.LogInformation("Completed mailbox migration from {Source} to {Destination}. " +
                    "Success: {Successful}, Failed: {Failed}",
                    sourceMailbox, destinationMailbox,
                    progressInfo.SuccessfulItems, progressInfo.FailedItems);

                // Return true if more than 90% successful
                return progressInfo.FailedItems < (totalItems * 0.1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate mailbox from {Source} to {Destination}",
                    sourceMailbox, destinationMailbox);
                return false;
            }
        }

        private Dictionary<string, string> ParseConnectionString(string connectionString)
        {
            var parameters = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(connectionString))
                return parameters;

            try
            {
                // Support both key=value format and JSON format
                if (connectionString.TrimStart().StartsWith("{"))
                {
                    // JSON format
                    var jsonParams = JsonSerializer.Deserialize<Dictionary<string, string>>(connectionString);
                    if (jsonParams != null)
                        return jsonParams;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse connection string as JSON, falling back to key=value format");
            }

            // Key=value format
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

        // Helper method to create a Graph client (for future implementation)
        private GraphServiceClient? CreateGraphClient(Dictionary<string, string> connectionParams)
        {
            try
            {
                var tenantId = connectionParams.GetValueOrDefault("TenantId");
                var clientId = connectionParams.GetValueOrDefault("ClientId");
                var clientSecret = connectionParams.GetValueOrDefault("ClientSecret");

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogWarning("Missing required Graph API credentials");
                    return null;
                }

                var options = new ClientSecretCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                };

                var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
                var graphServiceClient = new GraphServiceClient(clientSecretCredential);

                return graphServiceClient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Graph client");
                return null;
            }
        }
    }
}