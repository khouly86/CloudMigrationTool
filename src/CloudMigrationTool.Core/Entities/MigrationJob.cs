using System.ComponentModel.DataAnnotations;
using CloudMigrationTool.Core.Enums;

namespace CloudMigrationTool.Core.Entities
{
    public class MigrationJob : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public MigrationType Type { get; set; }
        
        public MigrationStatus Status { get; set; } = MigrationStatus.NotStarted;
        
        public int SourceConnectionId { get; set; }
        public virtual ConnectionSettings SourceConnection { get; set; } = null!;
        
        public int DestinationConnectionId { get; set; }
        public virtual ConnectionSettings DestinationConnection { get; set; } = null!;
        
        public string? Configuration { get; set; }
        
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public int TotalItems { get; set; } = 0;
        public int ProcessedItems { get; set; } = 0;
        public int SuccessfulItems { get; set; } = 0;
        public int FailedItems { get; set; } = 0;
        
        public string? ErrorMessage { get; set; }
        public string? LogFilePath { get; set; }
        
        // Navigation properties
        public virtual ICollection<MigrationJobLog> Logs { get; set; } = new List<MigrationJobLog>();
    }
}
