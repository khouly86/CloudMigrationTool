using CloudMigrationTool.Core.Entities;
using CloudMigrationTool.Core.Models;
using CloudMigrationTool.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CloudMigrationTool.Web.Models
{
    public class MigrationDashboardViewModel
    {
        public List<MigrationJob> ActiveJobs { get; set; } = new List<MigrationJob>();
        public List<MigrationJob> RecentJobs { get; set; } = new List<MigrationJob>();
        public int TotalJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int FailedJobs { get; set; }
        public int InProgressJobs { get; set; }
    }

    public class CreateMigrationJobViewModel
    {
        [Required]
        [Display(Name = "Job Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Migration Type")]
        public MigrationType Type { get; set; }

        [Required]
        [Display(Name = "Source Connection")]
        public int SourceConnectionId { get; set; }

        [Required]
        [Display(Name = "Destination Connection")]
        public int DestinationConnectionId { get; set; }

        [Display(Name = "Configuration (JSON)")]
        public string? Configuration { get; set; }

        public List<ConnectionSettings> Connections { get; set; } = new List<ConnectionSettings>();
    }

    public class MigrationJobDetailsViewModel
    {
        public MigrationJob Job { get; set; } = null!;
        public MigrationProgress? Progress { get; set; }
        public List<MigrationJobLog> Logs { get; set; } = new List<MigrationJobLog>();
    }
}
