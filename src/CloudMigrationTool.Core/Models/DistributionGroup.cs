namespace CloudMigrationTool.Core.Models
{
    public class DistributionGroup
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public bool ModeratedEnabled { get; set; }
        public List<string> ManagedBy { get; set; } = new List<string>();
        public DateTime? Created { get; set; }
    }
}