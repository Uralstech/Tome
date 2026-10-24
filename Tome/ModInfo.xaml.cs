using GamebananaAPI.Data;
using GamebananaAPI.Data.Submission;
using System.Diagnostics;
using System.Windows;

namespace Tome;

public sealed partial class ModInfo : Window
{
    private readonly SubmissionProfile _profile;

    public ModInfo(SubmissionProfile profile)
    {
        InitializeComponent();

        DataContext = profile;
        _profile = profile;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await LicenseWebView.EnsureCoreWebView2Async();
            LicenseWebView.NavigateToString(_profile.LicenseHtml ?? string.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                ex.Message,
                "Could not display license",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void WebViewBack_Click(object sender, RoutedEventArgs e)
    {
        if (LicenseWebView.CoreWebView2?.CanGoBack == true)
            LicenseWebView.CoreWebView2.GoBack();
    }

    private void WebViewForward_Click(object sender, RoutedEventArgs e)
    {
        if (LicenseWebView.CoreWebView2?.CanGoForward == true)
            LicenseWebView.CoreWebView2.GoForward();
    }

    private void AuthorLink_Click(object sender, RoutedEventArgs e)
    {
        using Process? _ = Process.Start(new ProcessStartInfo
        {
            FileName = _profile.Author.ProfileUrl,
            UseShellExecute = true
        });
    }

    private void StudioLink_Click(object sender, RoutedEventArgs e)
    {
        if (_profile.Studio is not SubmissionAuthor studio)
            return;

        using Process? _ = Process.Start(new ProcessStartInfo
        {
            FileName = studio.ProfileUrl,
            UseShellExecute = true
        });
    }

    private void ProfileLink_Click(object sender, RoutedEventArgs e)
    {
        using Process? _ = Process.Start(new ProcessStartInfo
        {
            FileName = _profile.ProfileUrl,
            UseShellExecute = true
        });
    }
}
