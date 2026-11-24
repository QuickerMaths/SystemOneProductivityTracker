using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SystemOneProductivityTracker.Application.Interfaces;

namespace SystemOneProductivityTracker.Application.Services;

public sealed class WpfAuthService : IAuthService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string? CurrentUserId { get; private set; }
    public string? CurrentUserEmail { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentUserId);

    public event Action? AuthenticationStateChanged;

    public WpfAuthService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            return new AuthResult(false, "Nieprawidłowy adres e-mail lub hasło.");
        }

        CurrentUserId = user.Id;
        CurrentUserEmail = user.Email;
        AuthenticationStateChanged?.Invoke();

        return new AuthResult(true);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var user = new IdentityUser { UserName = email, Email = email };
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return new AuthResult(false, errors);
        }

        CurrentUserId = user.Id;
        CurrentUserEmail = user.Email;
        AuthenticationStateChanged?.Invoke();

        return new AuthResult(true);
    }

    public void Logout()
    {
        CurrentUserId = null;
        CurrentUserEmail = null;
        AuthenticationStateChanged?.Invoke();
    }
}