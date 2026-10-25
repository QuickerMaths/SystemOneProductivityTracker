using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using SystemOneProductivityTracker.Infrastructure.Persistence;

namespace SystemOneProductivityTracker.Wpf;

public partial class MainWindow : Window
{
    private readonly IDbContextFactory<SystemOneProductivityTrackerDbContext> _dbFactory;

    public MainWindow(IDbContextFactory<SystemOneProductivityTrackerDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            bool canConnect = await db.Database.CanConnectAsync();
            int usersCount = await db.Users.CountAsync();

            Debug.WriteLine("=========================================");
            Debug.WriteLine($"[TEST DI & SQL] Połączenie z Azure SQL: {canConnect}");
            Debug.WriteLine($"[TEST DI & SQL] Liczba użytkowników w bazie: {usersCount}");
            Debug.WriteLine("=========================================");

            Title = $"Połączono z Azure SQL! (Users: {usersCount})";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BŁĄD KRYTYCZNY]: {ex.Message}");
            MessageBox.Show(ex.ToString(), "Błąd połączenia", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}