namespace SystemOneProductivityTracker.Domain.Entities;

public sealed class BlockEvent
{
    public string BlockEventId { get; set; } = Guid.NewGuid().ToString();
    public required DateTime OccuredAt { get; set; }
    public required BlockEventActionType ActionType { get; set; }

    #region Relations
    public required string UserId { get; set; }
    public string? PlannedBlockId { get; set; }
    public PlannedBlock? PlannedBlock { get; set; }
    public required string TrackedAppId { get; set; }
    public TrackedApp TrackedApp { get; set; } = null!;
    #endregion
}