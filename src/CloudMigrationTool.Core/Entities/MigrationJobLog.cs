using System.ComponentModel.DataAnnotations;

namespace CloudMigrationTool.Core.Entities
{
    public class MigrationJobLog : BaseEntity
    {
        [Required]
        public int MigrationJobId { get; set; }
        public virtual MigrationJob MigrationJob { get; set; } = null!;
        
        [Required]
        [MaxLength(50)]
        public string Level { get; set; } = string.Empty; // Info, Warning, Error
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? Details { get; set; }
        
        public string? Source { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
