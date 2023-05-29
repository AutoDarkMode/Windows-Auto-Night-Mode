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
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoDarkModeApp.Handlers;
using AutoDarkModeLib;
using ModernWpf.Media.Animation;
using AdmProperties = AutoDarkModeLib.Properties;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PagePersonalization.xaml
    /// </summary>
    public partial class PagePersonalization : ModernWpf.Controls.Page
    {
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        public PagePersonalization()
        {
            try
            {
                builder.Load();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("error loading config", ex);
            }
            InitializeComponent();

            if (builder.Config.WindowsThemeMode.Enabled)
            {
                SetThemePickerEnabled();
            }

            if (builder.Config.WallpaperSwitch.Enabled || builder.Config.ColorizationSwitch.Enabled || builder.Config.CursorSwitch.Enabled)
            {
                SelectAdmCustomizeEnabled();
            }
            if (!builder.Config.WallpaperSwitch.Enabled && !builder.Config.WindowsThemeMode.Enabled && !builder.Config.ColorizationSwitch.Enabled && !builder.Config.CursorSwitch.Enabled)
            {
                WallpaperDisabledMessage.Visibility = Visibility.Collapsed;
                ColorizationDisabledMessage.Visibility = Visibility.Collapsed;
                CursorsDisabledMessage.Visibility = Visibility.Collapsed;
                ThemeDisabledMessage.Visibility = Visibility.Collapsed;
            }

            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
            {
                FontIconColorization.FontFamily = new("Segoe Fluent Icons");
                FontIconTheme.FontFamily = new("Segoe Fluent Icons");
                FontIconWallpaper.FontFamily = new("Segoe Fluent Icons");
            }

            if (Environment.OSVersion.Version.Build < (int)WindowsBuilds.MinBuildForNewFeatures)
            {
                ColorizationPickerCard.Visibility = Visibility.Collapsed;
                CursorsPickerCard.Visibility = Visibility.Collapsed;
            }
        }

        private void SetThemePickerEnabled()
        {
            WallpaperDisabledMessage.Visibility = Visibility.Visible;
            WallpaperPickerCard.IsEnabled = false;

            ColorizationDisabledMessage.Visibility = Visibility.Visible;
            ColorizationPickerCard.IsEnabled = false;

            CursorsPickerCard.Visibility = Visibility.Visible;
            CursorsPickerCard.IsEnabled = false;

            ThemeDisabledMessage.Visibility = Visibility.Collapsed;
            ThemePickerCard.IsEnabled = true;

            if (builder.Config.ColorizationSwitch.Enabled)
            {
                builder.Config.ColorizationSwitch.Enabled = false;
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PersonalizationPage_SetThemePickerEnabled");
                }
            }

            if (builder.Config.WallpaperSwitch.Enabled)
            {
                builder.Config.WallpaperSwitch.Enabled = false;
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PersonalizationPage_SetThemePickerEnabled");
                }
            }
        }

        private void SelectAdmCustomizeEnabled()
        {
            CursorsDisabledMessage.Visibility = Visibility.Collapsed;
            CursorsPickerCard.IsEnabled = true;

            WallpaperDisabledMessage.Visibility = Visibility.Collapsed;
            WallpaperPickerCard.IsEnabled = true;

            ColorizationDisabledMessage.Visibility = Visibility.Collapsed;
            ColorizationPickerCard.IsEnabled = true;

            ThemeDisabledMessage.Visibility = Visibility.Visible;
            ThemePickerCard.IsEnabled = false;
        }

        private void HyperlinkThemeMode_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                ThemePickerCard_MouseDown(this, null);
            }
        }

        private void HyperlinkWallpaper_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                WallpaperPickerCard_MouseDown(this, null);
            }
        }

        private void ShowErrorMessage(String message, Exception ex)
        {
            string error = AdmProperties.Resources.ErrorMessageBox + $"\n\n{message}: " + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new(error, AdmProperties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
            var result = msg.DialogResult;
            if (result == true)
            {
                string issueUri = @"https://github.com/Armin2208/Windows-Auto-Night-Mode/issues";
                Process.Start(new ProcessStartInfo(issueUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            return;
        }

        private void WallpaperPickerCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Frame.Navigate(typeof(PageWallpaperPicker), null, new DrillInNavigationTransitionInfo());
        }

        private void ThemePickerCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Frame.Navigate(typeof(PageThemePicker), null, new DrillInNavigationTransitionInfo());
        }

        private void ColorizationPickerCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Frame.Navigate(typeof(PageColorization), null, new DrillInNavigationTransitionInfo());

        }

        private void CursorsPickerCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Frame.Navigate(typeof(PageCursors), null, new DrillInNavigationTransitionInfo());
        }
    }
}
