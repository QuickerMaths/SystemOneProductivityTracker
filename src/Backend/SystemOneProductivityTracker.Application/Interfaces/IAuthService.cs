namespace SystemOneProductivityTracker.Application.Interfaces;

public record AuthResult(bool Succeeded, string? ErrorMessage = null);

public interface IAuthService
{
    string? CurrentUserId { get; }
    string? CurrentUserEmail { get; }
    bool IsAuthenticated { get; }

    event Action? AuthenticationStateChanged;

    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string email, string password);
    void Logout();
}