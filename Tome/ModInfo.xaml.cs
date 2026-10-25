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
using System.Windows;
using Tome.Services;

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
        UrlHelper.Open(_profile.Author.ProfileUrl);

    private void StudioLink_Click(object sender, RoutedEventArgs e)
    {
        if (_profile.Studio is SubmissionAuthor studio)
            UrlHelper.Open(studio.ProfileUrl);
    }

    private void ProfileLink_Click(object sender, RoutedEventArgs e) =>
        UrlHelper.Open(_profile.ProfileUrl);
}
