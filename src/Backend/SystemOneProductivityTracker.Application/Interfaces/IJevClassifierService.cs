namespace SystemOneProductivityTracker.Application.Interfaces
{
    public record JevClassificationRequest(
        string ProcessName,
        string? SampleWindowTitle);

    public record JevClassificationResult(
        string? MatchedCategoryId,
        double Confidence,
        string? Resoning);

    public interface IJevClassficationService
    {
        Task ClassifyAppAsync(
            JevClassificationRequest request);
    }
}
