using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Tome;

public partial class MainWindow : Window
{
    public ObservableCollection<Mod> Mods { get; } = [];

    public MainWindow()
    {
        InitializeComponent();

        ModsGrid.ItemsSource = Mods;
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) =>
        Close();

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
}