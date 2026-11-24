using System.Windows.Input;
using SystemOneProductivityTracker.Application.Interfaces;
using SystemOneProductivityTracker.Wpf.Core;
using SystemOneProductivityTracker.Wpf.Services;

namespace SystemOneProductivityTracker.Wpf.ViewModels;

public sealed class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly IAuthService _authService;
    private readonly SessionRecorderService _sessionRecorder;

    private string _currentActivityName = "Waiting...";
    private string _currentActivityDuration = "00:00:00";

    public string WelcomeText => $"Zalogowany jako: {_authService.CurrentUserEmail}";

    public string CurrentActivityName
    {
        get => _currentActivityName;
        private set => SetProperty(ref _currentActivityName, value);
    }

    public string CurrentActivityDuration
    {
        get => _currentActivityDuration;
        private set => SetProperty(ref _currentActivityDuration, value);
    }

    public ICommand LogoutCommand { get; }
    public ICommand ActionCommand { get; }

    public DashboardViewModel(
        IAuthService authService,
        SessionRecorderService sessionRecorder)
    {
        _authService = authService;
        _sessionRecorder = sessionRecorder;

        _sessionRecorder.CurrentActivityUpdated += OnCurrentActivityUpdated;

        LogoutCommand = new AsyncRelayCommand(_ =>
        {
            _authService.Logout();
            return Task.CompletedTask;
        });
        ActionCommand = new AsyncRelayCommand(ExecuteActionAsync);
    }

    private void OnCurrentActivityUpdated(string activityName, TimeSpan elapsed)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CurrentActivityName = activityName;
            CurrentActivityDuration = elapsed.ToString(@"hh\:mm\:ss");
        });
    }

    private Task ExecuteActionAsync(object? parameter) => Task.CompletedTask;

    public void Dispose()
    {
        _sessionRecorder.CurrentActivityUpdated -= OnCurrentActivityUpdated;
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
        if (_currentViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        CurrentViewModel = _authService.IsAuthenticated
            ? _dashboardVmFactory()
            : _loginVmFactory();
    }
}