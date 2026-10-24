using GamebananaAPI.Data.Submission;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Tome.Models;
using Tome.Services;

namespace Tome;

public sealed partial class MainWindow : Window
{
    private readonly SemaphoreSlim _getSemaphore = new(1, 1);

    private readonly DeadlockService _deadlock;
    private readonly GamebananaService _gamebanana;

    private readonly ObservableCollection<ListedMod> _mods = [];
    private ToggleButton? _currentChecked = null;

    public MainWindow(DeadlockService deadlock, GamebananaService gamebanana)
    {
        InitializeComponent();

        _deadlock = deadlock;
        _gamebanana = gamebanana;
        ModsGrid.ItemsSource = _mods;
    }

    protected override void OnClosed(EventArgs e)
    {
        _getSemaphore.Dispose();

        base.OnClosed(e);
    }

    private void SetChecked(ToggleButton? newChecked)
    {
        if (ReferenceEquals(_currentChecked, newChecked))
            return;

        _currentChecked?.IsChecked = false;
        _currentChecked = newChecked;
    }

    private void SearchSafe()
    {
        SetChecked(null);
        RunModsUpdateSafe(() => _gamebanana.SearchAsync(SearchBox.Text));
    }

    private async void ShowModInfoSafeAsync(int id, string type)
    {
        await _getSemaphore.WaitAsync();
        try
        {
            SubmissionProfile submission = await _gamebanana.GetSubmissionAsync(id, type);
            ModInfo modInfoWindow = new(submission);
            modInfoWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                ex.Message,
                "Operation failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _getSemaphore.Release();
        }
    }

    private async void RunModsUpdateSafe(Func<Task<IReadOnlyList<ListedMod>>> func)
    {
        await _getSemaphore.WaitAsync();
        try
        {
            _mods.Clear();
            foreach (ListedMod mod in await func())
                _mods.Add(mod);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                ex.Message,
                "Operation failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _getSemaphore.Release();
        }
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
            SearchSafe();
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e) => SearchSafe();

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button
            || button.Tag is not ListedMod mod)
            return;

        ShowModInfoSafeAsync(mod.Id, mod.Type);
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

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        using Process? _ = Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/Uralstech/Tome",
            UseShellExecute = true
        });
    }

    private void LaunchDeadlock_Click(object sender, RoutedEventArgs e)
    {
        using Process? proc = _deadlock.LaunchDeadlock();
        if (proc is null)
        {
            MessageBox.Show("Could not start Deadlock.");
            return;
        }
    }
}