namespace SystemOneProductivityTracker.Domain.Entities;

public sealed class AppCategory
{
    public string AppCategoryId { get; set; } = Guid.NewGuid().ToString();
    public required string Name { get; set; }
    public required bool IsProductive { get; set; }
    public required string ColorHex { get; set; }

    #region Relations
    public string? UserId { get; set; }
    public ICollection<TrackedApp> TrackedApps { get; set; } = new List<TrackedApp>();
    #endregion
}
