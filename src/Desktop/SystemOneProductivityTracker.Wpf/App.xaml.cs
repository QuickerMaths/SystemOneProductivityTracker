using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SystemOneProductivityTracker.Application.Interfaces;
using SystemOneProductivityTracker.Application.Services;
using SystemOneProductivityTracker.Infrastructure;
using SystemOneProductivityTracker.Wpf.ViewModels;

namespace SystemOneProductivityTracker.Wpf;

public partial class App : System.Windows.Application   
{
    public IServiceProvider Services { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;

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
        mainWindow.Show();
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

        services.AddSingleton<MainWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
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
}