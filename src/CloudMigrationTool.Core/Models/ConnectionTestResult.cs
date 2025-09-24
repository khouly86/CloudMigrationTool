using CloudMigrationTool.Core.Enums;

namespace CloudMigrationTool.Core.Models
{
    public class ConnectionTestResult
    {
        public bool IsSuccessful { get; set; }
        public string? Message { get; set; }
        public ConnectionStatus Status { get; set; }
        public DateTime TestTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string>? AdditionalInfo { get; set; }
    }
}
