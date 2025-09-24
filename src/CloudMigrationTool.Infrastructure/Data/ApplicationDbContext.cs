using CloudMigrationTool.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudMigrationTool.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ConnectionSettings> ConnectionSettings { get; set; } = null!;
        public DbSet<MigrationJob> MigrationJobs { get; set; } = null!;
        public DbSet<MigrationJobLog> MigrationJobLogs { get; set; } = null!;
        public DbSet<AppSettings> AppSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ConnectionSettings configuration
            modelBuilder.Entity<ConnectionSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ConnectionString).IsRequired();
                
                entity.HasMany(e => e.MigrationJobs)
                      .WithOne()
                      .HasForeignKey(j => j.SourceConnectionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // MigrationJob configuration
            modelBuilder.Entity<MigrationJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                
                entity.HasOne(e => e.SourceConnection)
                      .WithMany()
                      .HasForeignKey(e => e.SourceConnectionId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(e => e.DestinationConnection)
                      .WithMany()
                      .HasForeignKey(e => e.DestinationConnectionId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasMany(e => e.Logs)
                      .WithOne(l => l.MigrationJob)
                      .HasForeignKey(l => l.MigrationJobId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // MigrationJobLog configuration
            modelBuilder.Entity<MigrationJobLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Level).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.HasIndex(e => new { e.MigrationJobId, e.Timestamp });
            });

            // AppSettings configuration
            modelBuilder.Entity<AppSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Key).IsUnique();
                entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(50);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default app settings
            modelBuilder.Entity<AppSettings>().HasData(
                new AppSettings
                {
                    Id = 1,
                    Key = "DefaultLogLevel",
                    Value = "Information",
                    Description = "Default logging level for the application",
                    Category = "Logging",
                    CreatedAt = DateTime.UtcNow
                },
                new AppSettings
                {
                    Id = 2,
                    Key = "MaxConcurrentMigrations",
                    Value = "3",
                    Description = "Maximum number of concurrent migration jobs",
                    Category = "Migration",
                    CreatedAt = DateTime.UtcNow
                },
                new AppSettings
                {
                    Id = 3,
                    Key = "MigrationTimeoutMinutes",
                    Value = "480",
                    Description = "Migration timeout in minutes (8 hours)",
                    Category = "Migration",
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
