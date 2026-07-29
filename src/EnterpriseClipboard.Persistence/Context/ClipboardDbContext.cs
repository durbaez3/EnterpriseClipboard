using Microsoft.EntityFrameworkCore;
using EnterpriseClipboard.Domain.Entities;

namespace EnterpriseClipboard.Persistence.Context;

public class ClipboardDbContext : DbContext
{
    public ClipboardDbContext(DbContextOptions<ClipboardDbContext> options) : base(options)
    {
    }

    public DbSet<ClipboardItem> ClipboardItems => Set<ClipboardItem>();
    public DbSet<ClipboardGroup> ClipboardGroups => Set<ClipboardGroup>();
    public DbSet<ClipboardTag> ClipboardTags => Set<ClipboardTag>();
    public DbSet<ClipboardItemTag> ClipboardItemTags => Set<ClipboardItemTag>();
    public DbSet<ApplicationExclusion> ApplicationExclusions => Set<ApplicationExclusion>();
    public DbSet<SensitiveDataRule> SensitiveDataRules => Set<SensitiveDataRule>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<HotkeyConfiguration> HotkeyConfigurations => Set<HotkeyConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Composite Key for ClipboardItemTag
        modelBuilder.Entity<ClipboardItemTag>()
            .HasKey(cit => new { cit.ClipboardItemId, cit.ClipboardTagId });

        // Configure AppSetting Primary Key
        modelBuilder.Entity<AppSetting>()
            .HasKey(s => s.Key);

        // Indexes for performance
        modelBuilder.Entity<ClipboardItem>()
            .HasIndex(c => c.CreatedAt);

        modelBuilder.Entity<ClipboardItem>()
            .HasIndex(c => c.ContentHash);

        modelBuilder.Entity<ClipboardItem>()
            .HasIndex(c => c.IsFavorite);

        modelBuilder.Entity<ClipboardItem>()
            .HasIndex(c => c.GroupId);

        modelBuilder.Entity<ClipboardItem>()
            .HasIndex(c => c.SourceApplication);

        modelBuilder.Entity<ClipboardItem>()
            .HasIndex(c => c.LastUsedAt);

        modelBuilder.Entity<ClipboardItem>()
            .HasIndex(c => c.ExpirationDate);
    }
}
