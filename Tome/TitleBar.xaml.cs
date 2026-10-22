using System.Windows;
using System.Windows.Controls;

namespace Tome;

public partial class TitleBar : UserControl
{
    public static readonly DependencyProperty MenuContentProperty =
        DependencyProperty.Register(
            nameof(MenuContent),
            typeof(object),
            typeof(TitleBar),
            new PropertyMetadata(null));

    public object? MenuContent
    {
        get => GetValue(MenuContentProperty);
        set => SetValue(MenuContentProperty, value);
    }

    public Window Window => Window.GetWindow(this);

    public TitleBar()
    {
        InitializeComponent();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        Window.WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        Window window = Window;
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e) =>
        Window.Close();
}
