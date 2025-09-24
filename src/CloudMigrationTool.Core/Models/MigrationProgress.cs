namespace CloudMigrationTool.Core.Models
{
    public class MigrationProgress
    {
        public int JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public double PercentageComplete => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
        public DateTime? StartTime { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public string? CurrentOperation { get; set; }
        public string? LastError { get; set; }
    }
}
