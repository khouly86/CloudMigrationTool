namespace CloudMigrationTool.Core.Models
{
    public class AdGroup
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string GroupType { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public string DistinguishedName { get; set; } = string.Empty;
        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }
    }
}