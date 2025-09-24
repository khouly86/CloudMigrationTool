namespace CloudMigrationTool.Core.Models
{
    public class AdOrganizationalUnit
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DistinguishedName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ParentOU { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int ComputerCount { get; set; }
        public int GroupCount { get; set; }
        public int ChildOUCount { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }
    }
}