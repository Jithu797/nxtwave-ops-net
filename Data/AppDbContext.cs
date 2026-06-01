using Microsoft.EntityFrameworkCore;
using LMSDashboard.Models;

namespace LMSDashboard.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ValidationLog> ValidationLogs => Set<ValidationLog>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<JobRecord> JobRecords => Set<JobRecord>();
    public DbSet<ReportCache> ReportCaches => Set<ReportCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ContentItem>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Track);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.StatusChangedAt);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Track).HasConversion<string>();
            entity.Property(e => e.Difficulty).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<ValidationLog>(entity =>
        {
            entity.HasOne(v => v.ContentItem)
                  .WithMany(c => c.ValidationLogs)
                  .HasForeignKey(v => v.ContentItemId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ContentItemId);
            entity.Property(e => e.Result).HasConversion<string>();
        });

        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.HasIndex(e => e.SyncedAt);
        });

        modelBuilder.Entity<ReportCache>(entity =>
        {
            entity.HasIndex(e => e.ReportKey).IsUnique();
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var items = new List<ContentItem>();
        var tracks = Enum.GetValues<Track>();
        var types = Enum.GetValues<ContentType>();
        var difficulties = Enum.GetValues<Difficulty>();
        var statuses = Enum.GetValues<ContentStatus>();
        var users = new[] { "alice@nxtwave.in", "bob@nxtwave.in", "carol@nxtwave.in" };

        var baseDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 1; i <= 20; i++)
        {
            var status = statuses[i % statuses.Length];
            var createdAt = baseDate.AddDays(i - 1).AddHours(i);
            items.Add(new ContentItem
            {
                Id = Guid.Parse($"00000000-0000-0000-0000-{i:D12}"),
                Title = $"Sample Content Item {i}",
                Type = types[i % types.Length],
                Track = tracks[i % tracks.Length],
                Difficulty = difficulties[i % difficulties.Length],
                Status = status,
                CreatedAt = createdAt,
                StatusChangedAt = createdAt,
                CreatedBy = users[i % users.Length],
                Notes = $"Seeded item {i} for demo",
                BetaUploadedAt = status >= ContentStatus.InBeta ? createdAt.AddDays(1) : null,
                ProdUploadedAt = status == ContentStatus.InProduction ? createdAt.AddDays(3) : null,
                ValidatedAt = status >= ContentStatus.Validated ? createdAt.AddDays(2) : null,
                IsDeleted = false
            });
        }

        modelBuilder.Entity<ContentItem>().HasData(items);
    }
}
