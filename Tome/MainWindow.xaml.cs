using GamebananaAPI;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Tome;

public partial class MainWindow : Window
{
    private readonly GamebananaService _gamebanana = new();
    private readonly SemaphoreSlim _getSemaphore = new(1, 1);

    private ToggleButton? _currentChecked = null;

    public ObservableCollection<Mod> Mods { get; } = [];

    public MainWindow()
    {
        InitializeComponent();

        ModsGrid.ItemsSource = Mods;
    }

    protected override void OnClosed(EventArgs e)
    {
        _gamebanana.Dispose();
        _getSemaphore.Dispose();

        base.OnClosed(e);
    }

    private async void RunModsUpdateSafe(Func<Task<IReadOnlyList<Mod>>> func)
    {
        await _getSemaphore.WaitAsync();
        try
        {
            Mods.Clear();
            foreach (Mod mod in await func())
                Mods.Add(mod);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                ex.ToString(),
                "Operation failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _getSemaphore.Release();
        }
    }

    private void SetChecked(ToggleButton? newChecked)
    {
        if (ReferenceEquals(_currentChecked, newChecked))
            return;

        _currentChecked?.IsChecked = false;
        _currentChecked = newChecked;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FeaturedButton.IsChecked = true;
        SetChecked(FeaturedButton);
    }

    private void Search_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is System.Windows.Input.Key.Enter or System.Windows.Input.Key.Return)
        {
            e.Handled = true;
            SetChecked(null);
            RunModsUpdateSafe(() => _gamebanana.SearchAsync(SearchBox.Text));
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        SetChecked(null);
        RunModsUpdateSafe(() => _gamebanana.SearchAsync(SearchBox.Text));
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
    }

    private void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton selectedButton)
            return;

        SetChecked(selectedButton);

        if (ReferenceEquals(sender, FeaturedButton))
            RunModsUpdateSafe(() => _gamebanana.GetFeaturedAsync());
        else if (ReferenceEquals(sender, TopSubmissionsButton))
            RunModsUpdateSafe(() => _gamebanana.GetTopSubmissionsAsync());
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) =>
        Close();
}