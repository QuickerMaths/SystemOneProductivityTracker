using System.Windows.Controls;
using System.Windows.Input;
using SystemOneProductivityTracker.Application.Interfaces;
using SystemOneProductivityTracker.Wpf.Core;

namespace SystemOneProductivityTracker.Wpf.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private string _email = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand RegisterCommand { get; }

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;

        LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteAuth);
        RegisterCommand = new AsyncRelayCommand(ExecuteRegisterAsync, CanExecuteAuth);
    }

    private bool CanExecuteAuth(object? parameter) => !IsBusy && !string.IsNullOrWhiteSpace(Email);

    private async Task ExecuteLoginAsync(object? parameter)
    {
        var password = (parameter as PasswordBox)?.Password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Podaj hasło.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        var result = await _authService.LoginAsync(Email.Trim(), password);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Błąd logowania.";
        }

        IsBusy = false;
    }

    private async Task ExecuteRegisterAsync(object? parameter)
    {
        var password = (parameter as PasswordBox)?.Password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Podaj hasło do rejestracji.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        var result = await _authService.RegisterAsync(Email.Trim(), password);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Błąd rejestracji.";
        }

        IsBusy = false;
    }
}