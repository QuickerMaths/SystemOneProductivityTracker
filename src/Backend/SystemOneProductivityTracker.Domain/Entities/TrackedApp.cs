namespace SystemOneProductivityTracker.Domain.Entities;

public sealed class TrackedApp
{
    public string TrackedAppId { get; set; } = Guid.NewGuid().ToString();
    public required string ProcessName { get; set; }
    public string? DisplayName { get; set; }
    public required bool IsAllwaysAllowed { get; set; }

    #region Relations
    public required string UserId { get; set; }
    public string? AppCategoryId { get; set; }
    public AppCategory? AppCategory { get; set; }
    public ICollection<ActivitySession> ActivitySessions { get; set; } = new List<ActivitySession>();
    #endregion
}