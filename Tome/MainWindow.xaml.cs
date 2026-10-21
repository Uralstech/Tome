using GamebananaAPI;
using GamebananaAPI.Data.Game;
using GamebananaAPI.Data.Game.Featured;
using GamebananaAPI.Data.Game.TopSubs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Tome;

public partial class MainWindow : Window
{
    private const int DeadlockGameId = 20948;

    public ObservableCollection<Mod> Mods { get; } = [];

    public MainWindow()
    {
        InitializeComponent();

        ModsGrid.ItemsSource = Mods;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await GetFeatured();
    }

    private async Task GetTopSubmissions()
    {
        static int GetPriority(TopSubmissionPeriod group) => group switch
        {
            TopSubmissionPeriod.Today => 0,
            TopSubmissionPeriod.ThisWeek => 1,
            TopSubmissionPeriod.ThisMonth => 2,
            TopSubmissionPeriod.ThreeMonths => 3,
            TopSubmissionPeriod.SixMonths => 4,
            TopSubmissionPeriod.ThisYear => 5,
            TopSubmissionPeriod.AllTime => 6,
            _ => int.MaxValue
        };

        List<TopSubmission> top = await Gamebanana.Games.Search.TopSubmissionsAsync(DeadlockGameId);
        Mods.Clear();

        IOrderedEnumerable<TopSubmission> topOrdered = top.OrderBy(static sub => GetPriority(sub.Period));
        foreach (TopSubmission submission in topOrdered)
        {
            if (submission.SafeForWork is not GamebananaAPI.Data.Game.Visibility.Show)
                continue;

            Mods.Add(new Mod()
            {
                Id = submission.Id,
                Name = submission.Name,
                Likes = submission.Likes,
                Author = submission.Author.Name
            });
        }
    }

    private async Task GetFeatured()
    {
        static int GetPriority(FeaturingGroup group) => group switch
        {
            FeaturingGroup.Today => 0,
            FeaturingGroup.Yesterday => 1,
            FeaturingGroup.ThreeDays => 2,
            _ => int.MaxValue
        };

        FeaturedSubmissions featured = await Gamebanana.Games.Search.FeaturedAsync(DeadlockGameId);
        Mods.Clear();

        IOrderedEnumerable<FeaturedSubmission> featuredOrdered = featured.Submissions
            .OrderBy(static sub =>
                sub.Featurings.OrderBy(static feat => GetPriority(feat.Group))
                .ThenByDescending(static feat => feat.Date)
                .Select(static feat => GetPriority(feat.Group))
                .FirstOrDefault(int.MaxValue));

        foreach (FeaturedSubmission submission in featuredOrdered)
        {
            if (submission.SafeForWork is not GamebananaAPI.Data.Game.Visibility.Show)
                continue;

            Mods.Add(new Mod()
            {
                Id = submission.Id,
                Name = submission.Name,
                Likes = submission.Likes,
                Author = submission.Author.Name
            });
        }
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
    }

    private void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton selectedButton
            || selectedButton.Parent is not Panel parent)
            return;

        foreach (object? child in parent.Children)
        {
            if (child is ToggleButton button
                && button != selectedButton)
                button.IsChecked = false;
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) =>
        Close();
}