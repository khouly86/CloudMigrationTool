namespace CloudMigrationTool.Core.Models
{
    public class AdUser
    {
        public string? SamAccountName { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? DisplayName { get; set; }
        public string? GivenName { get; set; }
        public string? Surname { get; set; }
        public string? EmailAddress { get; set; }
        public string? Department { get; set; }
        public string? Title { get; set; }
        public string? Manager { get; set; }
        public bool Enabled { get; set; }
        public DateTime? LastLogon { get; set; }
        public DateTime? PasswordLastSet { get; set; }
        public string? DistinguishedName { get; set; }
        public List<string> MemberOf { get; set; } = new List<string>();
    }
}
