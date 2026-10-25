using System.Windows;
using SystemOneProductivityTracker.Wpf.ViewModels;

namespace SystemOneProductivityTracker.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
    }
}