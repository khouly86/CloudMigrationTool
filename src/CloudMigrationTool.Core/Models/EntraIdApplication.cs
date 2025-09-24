namespace CloudMigrationTool.Core.Models
{
    public class EntraIdApplication
    {
        public string Id { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SignInAudience { get; set; } = string.Empty;
        public List<string> ReplyUrls { get; set; } = new List<string>();
        public List<string> RequiredResourceAccess { get; set; } = new List<string>();
        public DateTime? Created { get; set; }
        public bool IsEnabled { get; set; }
    }
}