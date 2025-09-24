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
