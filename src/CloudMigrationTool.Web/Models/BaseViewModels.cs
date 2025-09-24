using CloudMigrationTool.Core.Entities;

namespace CloudMigrationTool.Web.Models
{
    public class HomeIndexViewModel
    {
        public int ActiveMigrationsCount { get; set; }
        public int TotalJobsCount { get; set; }
        public int CompletedJobsCount { get; set; }
        public int FailedJobsCount { get; set; }
        public int ActiveConnectionsCount { get; set; }
        public int TotalConnectionsCount { get; set; }
        public List<MigrationJob> RecentJobs { get; set; } = new List<MigrationJob>();
    }
}