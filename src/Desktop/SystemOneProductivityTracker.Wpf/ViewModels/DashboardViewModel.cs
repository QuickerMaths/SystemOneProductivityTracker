using System.Windows.Input;
using SystemOneProductivityTracker.Application.Interfaces;
using SystemOneProductivityTracker.Wpf.Core;

namespace SystemOneProductivityTracker.Wpf.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    public string WelcomeText => $"Zalogowany jako: {_authService.CurrentUserEmail}";
    public ICommand LogoutCommand { get; }

    public DashboardViewModel(IAuthService authService)
    {
        _authService = authService;
        LogoutCommand = new AsyncRelayCommand(_ =>
        {
            _authService.Logout();
            return Task.CompletedTask;
        });
    }
}

public sealed class MainViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly Func<LoginViewModel> _loginVmFactory;
    private readonly Func<DashboardViewModel> _dashboardVmFactory;
    private ViewModelBase _currentViewModel = null!;

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public MainViewModel(
        IAuthService authService,
        Func<LoginViewModel> loginVmFactory,
        Func<DashboardViewModel> dashboardVmFactory)
    {
        _authService = authService;
        _loginVmFactory = loginVmFactory;
        _dashboardVmFactory = dashboardVmFactory;

        _authService.AuthenticationStateChanged += OnAuthStateChanged;
        OnAuthStateChanged();
    }

    private void OnAuthStateChanged()
    {
        CurrentViewModel = _authService.IsAuthenticated
            ? _dashboardVmFactory()
            : _loginVmFactory();
    }
}