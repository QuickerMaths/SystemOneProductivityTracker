using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using SystemOneProductivityTracker.Application.Interfaces;
using SystemOneProductivityTracker.Application.Services;
using SystemOneProductivityTracker.Infrastructure;
using SystemOneProductivityTracker.Wpf.Services;
using SystemOneProductivityTracker.Wpf.ViewModels;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace SystemOneProductivityTracker.Wpf;

public partial class App : System.Windows.Application
{
    public IServiceProvider Services { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;
    private NotifyIcon? _notifyIcon;
    private bool _isExit;

    public new static App Current => (App)System.Windows.Application.Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        Configuration = configBuilder.Build();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        Services = serviceCollection.BuildServiceProvider();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        _ = Services.GetRequiredService<ActivityMonitorService>();
        _ = Services.GetRequiredService<SessionRecorderService>();

        MainWindow.Closing += MainWindow_Closing;
        SetupTrayIcon();
        CreateContextMenu();
    }


    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Configuration);

        services.AddInfrastructure(Configuration);

        services.AddSingleton<IAuthService, WpfAuthService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddSingleton<Func<LoginViewModel>>(sp => () => sp.GetRequiredService<LoginViewModel>());
        services.AddSingleton<Func<DashboardViewModel>>(sp => () => sp.GetRequiredService<DashboardViewModel>());
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ActivityMonitorService>();
        services.AddSingleton<SessionRecorderService>();

        services.AddSingleton<MainWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var recorder = Services.GetService<SessionRecorderService>();
        if(recorder is not null)
        {
            await recorder.FlushCurrentSessionToDbAsync();
        }

        if (Services is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new NotifyIcon();
        _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
        _notifyIcon.Click += (s, args) => ShowMainWindow();
        _notifyIcon.Icon = ProjectResources.systemone_icon;
        _notifyIcon.Visible = true;
    }

    private void CreateContextMenu()
    {
        _notifyIcon?.ContextMenuStrip = new ContextMenuStrip();
        _notifyIcon?.ContextMenuStrip?.Items.Add("Open").Click += (s, e) => ShowMainWindow();
        _notifyIcon?.ContextMenuStrip?.Items.Add("Exit").Click += (s, e) => ExitApplication();
    }


    private void ShowMainWindow()
    {
        if (MainWindow.IsVisible)
        {
            if (MainWindow?.WindowState == WindowState.Minimized)
            {
                MainWindow.WindowState = WindowState.Normal;
            }
            MainWindow?.Activate();
        }
        else
        {
            MainWindow?.Show();
        }
    }

    void TrayIconOnClick(object? sender, EventArgs e) =>
        ShowMainWindow();

    private void ExitApplication()
    {
        _isExit = true;
        MainWindow?.Close();
        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }

    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        if (!_isExit)
        {
            e.Cancel = true;
            MainWindow.Hide();
        }
    }
}