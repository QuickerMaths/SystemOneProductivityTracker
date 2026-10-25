namespace SystemOneProductivityTracker.Domain.Entities;

public sealed class PlannedBlock
{
    public string PlannedBlockId { get; set; } = Guid.NewGuid().ToString();
    public required string Title { get; set; }
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public required BlockEventActionType StrictMode { get; set; }

    #region Relations
    public required string UserId { get; set; }
    public ICollection<AppCategory> AllowedCategories { get; set; } = new List<AppCategory>();
    public ICollection<TrackedApp> AllowedApps { get; set; } = new List<TrackedApp>();
    public ICollection<BlockEvent> BlockEvents { get; set; } = new List<BlockEvent>();
    #endregion
}