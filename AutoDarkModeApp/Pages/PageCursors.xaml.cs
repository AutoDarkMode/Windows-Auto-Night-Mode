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
using AutoDarkModeApp.Handlers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using ModernWpf.Media.Animation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageCursors.xaml
    /// </summary>
    public partial class PageCursors : ModernWpf.Controls.Page
    {
        bool init = true;
        private static AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        public PageCursors()
        {
            InitializeComponent();

            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
            {
                PointerSettingsCardIcon.FontFamily = new("Segoe Fluent Icons");
            }
        }

        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private void UpdateLightCursorPreviews(string scheme)
        {
            LightImagePanel.Children.Clear();
            string[] cursors = CursorCollectionHandler.GetCursorScheme(scheme);
            if (cursors.Length == 0)
            {
                LightImagePanelSeparator.Visibility = Visibility.Collapsed;
                return;
            }
            LightImagePanelSeparator.Visibility = Visibility.Visible;
            LightImagePanel.Visibility = Visibility.Visible;
            foreach (string cursor in cursors)
            {
                try
                {
                    Icon i = Icon.ExtractAssociatedIcon(cursor);
                    Bitmap b = i.ToBitmap();

                    System.Windows.Controls.Image im = new()
                    {
                        Source = ToBitmapImage(b),
                        Margin = new(4, 10, 4, 0),
                        MaxHeight = 32,
                        Stretch = Stretch.Uniform
                    };
                    LightImagePanel.Children.Add(im);
                }
                catch { }
            }
        }

        private void UpdateDarkCursorPreviews(string scheme)
        {
            DarkImagePanel.Children.Clear();
            string[] cursors = CursorCollectionHandler.GetCursorScheme(scheme);
            if (cursors.Length == 0)
            {
                DarkImagePanelSeparator.Visibility = Visibility.Collapsed;
                return;
            }
            DarkImagePanelSeparator.Visibility = Visibility.Visible;
            DarkImagePanel.Visibility = Visibility.Visible;
            foreach (string cursor in cursors)
            {
                try
                {
                    Icon i = Icon.ExtractAssociatedIcon(cursor);
                    Bitmap b = i.ToBitmap();

                    System.Windows.Controls.Image im = new()
                    {
                        Source = ToBitmapImage(b),
                        Margin = new(4, 10, 4, 0),
                        MaxHeight = 32,
                        Stretch = Stretch.Uniform
                    };
                    DarkImagePanel.Children.Add(im);
                }
                catch { }
            }
        }

        private void PointerSettingsLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start("control", "main.cpl,,1");
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "run mouse dialog");
            }
        }

        private async void ToggleSwitchCursors_Toggled(object sender, RoutedEventArgs e)
        {
            CursorsComboBoxLight.IsEnabled = ToggleSwitchCursors.IsOn;
            CursorsComboBoxDark.IsEnabled = ToggleSwitchCursors.IsOn;

            if (init) return;

            if (ToggleSwitchCursors.IsOn)
            {
                builder.Config.CursorSwitch.Enabled = true;
            }
            else
            {
                builder.Config.CursorSwitch.Enabled = false;
            }
            try
            {
                builder.Save();
                await RequestSwitch();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "ToggleCursors");
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> cursors = CursorCollectionHandler.GetCursors();
                CursorsComboBoxDark.ItemsSource = cursors;
                CursorsComboBoxLight.ItemsSource = cursors;

                if (builder.Config.CursorSwitch.Component.CursorsLight != null)
                {
                    CursorsComboBoxLight.SelectedItem = builder.Config.CursorSwitch.Component.CursorsLight;
                }
                else
                {
                    CursorsComboBoxLight.SelectedItem = CursorCollectionHandler.GetCurrentCursorScheme();
                }

                if (builder.Config.CursorSwitch.Component.CursorsDark != null)
                {
                    CursorsComboBoxDark.SelectedItem = builder.Config.CursorSwitch.Component.CursorsDark;
                }
                else
                {
                    CursorsComboBoxDark.SelectedItem = CursorCollectionHandler.GetCurrentCursorScheme();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageCursorsInitializeComboBoxes");
            }

            if (!builder.Config.CursorSwitch.Enabled)
            {
                CursorsComboBoxDark.IsEnabled = false;
                CursorsComboBoxLight.IsEnabled = false;
            }

            ToggleSwitchCursors.IsOn = builder.Config.CursorSwitch.Enabled;

            UpdateLightCursorPreviews((string)CursorsComboBoxLight.SelectedItem);
            UpdateDarkCursorPreviews((string)CursorsComboBoxDark.SelectedItem);

            init = false;
        }

        private void TextBlockBackButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Frame.Navigate(typeof(PagePersonalization), null, new DrillInNavigationTransitionInfo());
        }

        private void CursorsComboBoxLight_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                List<string> cursors = CursorCollectionHandler.GetCursors();
                CursorsComboBoxLight.ItemsSource = cursors;
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "RefreshCursors");
            }
        }

        private async void CursorsComboBoxLight_DropDownClosed(object sender, EventArgs e)
        {
            try
            {
                builder.Config.CursorSwitch.Component.CursorsLight = (string)CursorsComboBoxLight.SelectedItem;
                builder.Save();
                await RequestSwitch();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "SaveLightCursors");
            }

        }

        private async void CursorsComboBoxDark_DropDownClosed(object sender, EventArgs e)
        {
            try
            {
                builder.Config.CursorSwitch.Component.CursorsDark = (string)CursorsComboBoxDark.SelectedItem;
                builder.Save();
                await RequestSwitch();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "SaveDarkCursors");
            }
        }

        private void CursorsComboBoxDark_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                List<string> cursors = CursorCollectionHandler.GetCursors();
                CursorsComboBoxDark.ItemsSource = cursors;
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "RefreshCursors");
            }
        }

        private async Task RequestSwitch()
        {
            string result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException("Api " + result, "PageCursors");
            }
            UpdateLightCursorPreviews(builder.Config.CursorSwitch.Component.CursorsLight);
            UpdateDarkCursorPreviews(builder.Config.CursorSwitch.Component.CursorsDark);

        }
    }
}
