namespace CloudMigrationTool.Core.Models
{
    public class EntraIdRole
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsBuiltIn { get; set; }
        public bool IsEnabled { get; set; }
        public int MemberCount { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }
}