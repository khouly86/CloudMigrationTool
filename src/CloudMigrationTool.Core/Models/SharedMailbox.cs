namespace CloudMigrationTool.Core.Models
{
    public class SharedMailbox
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public int ItemCount { get; set; }
        public List<string> Delegates { get; set; } = new List<string>();
        public DateTime? LastAccessed { get; set; }
    }
}