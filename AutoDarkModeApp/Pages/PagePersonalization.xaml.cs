
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using AutoDarkModeConfig;
using ModernWpf.Media.Animation;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PagePersonalization.xaml
    /// </summary>
    public partial class PagePersonalization : ModernWpf.Controls.Page
    {
        private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
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

            if (builder.Config.WindowsThemeMode.Enabled & !builder.Config.WallpaperSwitch.Enabled)
            {
                SetThemePickerEnabled();
            }

            if (builder.Config.WallpaperSwitch.Enabled & !builder.Config.WindowsThemeMode.Enabled)
            {
                SetWallpaperPickerEnabled();
            }
            if (!builder.Config.WallpaperSwitch.Enabled & !builder.Config.WindowsThemeMode.Enabled)
            {
                WallpaperDisabledMessage.Visibility = Visibility.Collapsed;
                ThemeDisabledMessage.Visibility = Visibility.Collapsed;
            }
        }

        private void SetThemePickerEnabled()
        {
            WallpaperDisabledMessage.Visibility = Visibility.Visible;
            WallpaperPickerGrid.IsEnabled = false;

            ThemeDisabledMessage.Visibility = Visibility.Collapsed;
            ThemePickerGrid.IsEnabled = true;
        }

        private void SetWallpaperPickerEnabled ()
        {
            WallpaperDisabledMessage.Visibility = Visibility.Collapsed;
            WallpaperPickerGrid.IsEnabled = true;

            ThemeDisabledMessage.Visibility = Visibility.Visible;
            ThemePickerGrid.IsEnabled = false;
        }

        private void NavigateThemePicker(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PageThemePicker), null, new DrillInNavigationTransitionInfo());
        }

        private void NavigateWallpaperPicker(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PageWallpaperPicker), null, new DrillInNavigationTransitionInfo());
        }

        private void ShowErrorMessage(String message, Exception ex)
        {
            string error = Properties.Resources.errorThemeApply + $"\n\n{message}: " + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
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
    }
}
