#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using SourceChord.FluentWPF;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageDonation.xaml
    /// </summary>
    public partial class PageDonation : Page
    {
        public PageDonation()
        {
            InitializeComponent();

            SystemTheme.ThemeChanged += SystemTheme_ThemeChanged;
            SystemTheme_ThemeChanged(this, null);
        }

        private void SystemTheme_ThemeChanged(object sender, EventArgs e)
        {
            if (SystemTheme.AppTheme.Equals(ApplicationTheme.Dark))
            {
                gitHubImage.Source = new BitmapImage(new Uri(@"/Resources/GitHub_Logo_White.png", UriKind.Relative));
            }
            else
            {
                gitHubImage.Source = new BitmapImage(new Uri(@"/Resources/GitHub_Logo_Black.png", UriKind.Relative));
            }
        }
        private static void StartProcessByProcessInfo(string message)
        {
            Process.Start(new ProcessStartInfo(message)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void ButtonPayPal_Click(object sender, RoutedEventArgs e)
        {
            StartProcessByProcessInfo("https://www.paypal.com/donate/?hosted_button_id=H65KZYMHKCB6E");
        }

        private void ButtonGitHubSponsors_Click(object sender, RoutedEventArgs e)
        {
            StartProcessByProcessInfo("https://github.com/sponsors/Spiritreader");
        }

        private void ButtonKofi_Click(object sender, RoutedEventArgs e)
        {
            StartProcessByProcessInfo("https://ko-fi.com/spiritreader");
        }
    }
}
