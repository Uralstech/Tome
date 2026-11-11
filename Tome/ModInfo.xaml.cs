// Copyright 2026 URAV ADVANCED LEARNING SYSTEMS PRIVATE LIMITED
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GamebananaAPI.Data;
using GamebananaAPI.Data.Submission;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using System.Drawing;
using System.IO;
using System.Windows;
using Tome.Services;

namespace Tome;

public sealed partial class ModInfo : Window
{
    private readonly FileService _files;

    private readonly SubmissionProfile _profile;

    public ModInfo(SubmissionProfile profile)
    {
        InitializeComponent();

        DataContext = profile;
        _profile = profile;

        _files = App.Services.GetRequiredService<FileService>();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            string appData = _files.GetSaveDirectory();
            string webViewDir = Path.Combine(appData, "WebView2");
            Directory.CreateDirectory(webViewDir);

            CoreWebView2Environment webViewEnv = await CoreWebView2Environment.CreateAsync(userDataFolder: webViewDir);

            await LicenseWebView.EnsureCoreWebView2Async(webViewEnv);
            LicenseWebView.DefaultBackgroundColor = Color.Transparent;

            LicenseWebView.NavigateToString(_profile.LicenseHtml ?? string.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not display license",
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

    private void AuthorLink_Click(object sender, RoutedEventArgs e) =>
        UrlUtil.Open(_profile.Author.ProfileUrl);

    private void StudioLink_Click(object sender, RoutedEventArgs e)
    {
        if (_profile.Studio is SubmissionAuthor studio)
            UrlUtil.Open(studio.ProfileUrl);
    }

    private void ProfileLink_Click(object sender, RoutedEventArgs e) =>
        UrlUtil.Open(_profile.ProfileUrl);
}
