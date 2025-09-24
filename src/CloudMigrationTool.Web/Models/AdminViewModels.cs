using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CloudMigrationTool.Web.Models
{
    public class AdminDashboardViewModel
    {
        public List<ConnectionSettings> Connections { get; set; } = new List<ConnectionSettings>();
        public List<AppSettings> Settings { get; set; } = new List<AppSettings>();
    }

    public class ConnectionSettingsViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Connection Name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Connection Type")]
        public ConnectionType Type { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Connection String")]
        public string ConnectionString { get; set; } = string.Empty;

        [Display(Name = "Additional Settings")]
        public string? AdditionalSettings { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
