namespace SystemOneProductivityTracker.Domain.Entities;

public sealed class ActivitySession
{
    public string ActivitySessionId { get; set; } = Guid.NewGuid().ToString();
    public string? WindowTitle { get; set; }
    public required DateTime StartedAt { get; set; }
    public required DateTime EndedAt { get; set; }
    public int DurationSeconds { get; private set; }

    #region relations
    public required string UserId { get; set; }
    public required string TrackedAppId { get; set; }
    public TrackedApp TrackedApp { get; set; } = null!;
    #endregion
}