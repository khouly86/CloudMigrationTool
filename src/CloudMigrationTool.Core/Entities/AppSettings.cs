using System.ComponentModel.DataAnnotations;

namespace CloudMigrationTool.Core.Entities
{
    public class AppSettings : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(50)]
        public string? Category { get; set; }
        
        public bool IsEncrypted { get; set; } = false;
        
        public bool IsReadOnly { get; set; } = false;
    }
}
