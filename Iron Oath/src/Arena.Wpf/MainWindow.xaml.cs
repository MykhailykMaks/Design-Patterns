using System.Windows;
using System.Windows.Input;
using Arena.Wpf.ViewModels;

namespace Arena.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Width = Math.Min(1360, SystemParameters.WorkArea.Width * .9);
        Height = Math.Min(900, SystemParameters.WorkArea.Height * .9);
        DataContext = new MainViewModel();
    }

    private void FitScene(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Height <= 0 || e.NewSize.Width <= 0) return;
        var aspect = e.NewSize.Width / e.NewSize.Height;
        Scene.Width = Math.Max(1360, 900 * aspect);
        Scene.Height = Math.Max(900, 1360 / aspect);
    }

    private void HandleWindowKey(object sender, KeyEventArgs e)
    {
        var fullscreen = WindowStyle == WindowStyle.None;
        if (e.Key != Key.F11 && !(fullscreen && e.Key == Key.Escape)) return;
        if (fullscreen)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
        else
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
        e.Handled = true;
    }
}
