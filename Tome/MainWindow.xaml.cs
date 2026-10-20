using System.Collections.ObjectModel;
using System.Windows;

namespace Tome;

public partial class MainWindow : Window
{
    public ObservableCollection<Mod> Mods { get; } = [];

    public MainWindow()
    {
        InitializeComponent();

        ModsGrid.ItemsSource = Mods;
    }
}