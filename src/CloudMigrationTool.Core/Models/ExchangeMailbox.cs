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
