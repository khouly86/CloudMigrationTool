using System.ComponentModel.DataAnnotations;
using CloudMigrationTool.Core.Enums;

namespace CloudMigrationTool.Core.Entities
{
    public class ConnectionSettings : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public ConnectionType Type { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public string ConnectionString { get; set; } = string.Empty;
        
        public string? AdditionalSettings { get; set; }
        
        public ConnectionStatus Status { get; set; } = ConnectionStatus.NotConfigured;
        
        public DateTime? LastTestedAt { get; set; }
        
        public string? LastTestResult { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<MigrationJob> MigrationJobs { get; set; } = new List<MigrationJob>();
    }
}
