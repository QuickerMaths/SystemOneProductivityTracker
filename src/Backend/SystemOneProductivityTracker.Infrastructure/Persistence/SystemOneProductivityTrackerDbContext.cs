using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemOneProductivityTracker.Domain.Entities;

namespace SystemOneProductivityTracker.Infrastructure.Persistence;

public sealed class SystemOneProductivityTrackerDbContext : IdentityDbContext<IdentityUser>
{
    public SystemOneProductivityTrackerDbContext(
        DbContextOptions<SystemOneProductivityTrackerDbContext> options) : base(options)
    {
    }

    public DbSet<ActivitySession> ActivitySessions => Set<ActivitySession>();
    public DbSet<AppCategory> AppCategories => Set<AppCategory>();
    public DbSet<BlockEvent> BlockEvents => Set<BlockEvent>();
    public DbSet<PlannedBlock> PlannedBlocks => Set<PlannedBlock>();
    public DbSet<TrackedApp> TrackedApps => Set<TrackedApp>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(SystemOneProductivityTrackerDbContext).Assembly);
    }
}

internal sealed class ActivitySessionConfiguration : IEntityTypeConfiguration<ActivitySession>
{
    public void Configure(EntityTypeBuilder<ActivitySession> builder)
    {
        builder.HasKey(e => e.ActivitySessionId);

        builder
            .Property(e => e.ActivitySessionId)
            .HasMaxLength(36);

        builder.Property(e => e.WindowTitle)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.StartedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(e => e.EndedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(e => e.DurationSeconds)
            .HasComputedColumnSql("DATEDIFF(SECOND, [StartedAt], [EndedAt])", stored: true);

        builder.HasOne(e => e.TrackedApp)
            .WithMany(app => app.ActivitySessions)
            .HasForeignKey(e => e.TrackedAppId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.StartedAt });
    }
}

internal sealed class TrackedAppConfiguration : IEntityTypeConfiguration<TrackedApp>
{
    public void Configure(EntityTypeBuilder<TrackedApp> builder)
    {
        builder.HasKey(e => e.TrackedAppId);

        builder
            .Property(e => e.TrackedAppId)
            .HasMaxLength(36);

        builder.Property(e => e.ProcessName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(e => e.IsAllwaysAllowed)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasOne(e => e.AppCategory)
            .WithMany(c => c.TrackedApps)
            .HasForeignKey(e => e.AppCategoryId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.ProcessName })
            .IsUnique();
    }
}

internal sealed class AppCategoryConfiguration : IEntityTypeConfiguration<AppCategory>
{
    public void Configure(EntityTypeBuilder<AppCategory> builder)
    {
        builder.HasKey(e => e.AppCategoryId);

        builder
            .Property(e => e.AppCategoryId)
            .HasMaxLength(36);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.IsProductive)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.ColorHex)
            .HasMaxLength(7)
            .HasDefaultValue("#3B82F6")
            .IsRequired();

        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}

internal sealed class BlockEventConfiguration : IEntityTypeConfiguration<BlockEvent>
{
    public void Configure(EntityTypeBuilder<BlockEvent> builder)
    {
        builder.HasKey(e => e.BlockEventId);

        builder
            .Property(e => e.BlockEventId)
            .HasMaxLength(36);

        builder.Property(e => e.OccuredAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(e => e.ActionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(e => e.PlannedBlock)
            .WithMany(b => b.BlockEvents)
            .HasForeignKey(e => e.PlannedBlockId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(e => e.TrackedApp)
            .WithMany()
            .HasForeignKey(e => e.TrackedAppId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}

internal sealed class PlannedBlockConfiguration : IEntityTypeConfiguration<PlannedBlock>
{
    public void Configure(EntityTypeBuilder<PlannedBlock> builder)
    {
        builder.HasKey(e => e.PlannedBlockId);

        builder
            .Property(e => e.PlannedBlockId)
            .HasMaxLength(36);

        builder.Property(e => e.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.StartTime)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(e => e.EndTime)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(e => e.StrictMode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasMany(e => e.AllowedCategories)
            .WithMany();

        builder.HasMany(e => e.AllowedApps)
            .WithMany();

        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}