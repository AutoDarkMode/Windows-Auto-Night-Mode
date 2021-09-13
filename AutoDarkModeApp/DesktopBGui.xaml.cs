using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AutoDarkModeConfig;
using Microsoft.Win32;
using System.Collections.Generic;
using AutoDarkModeConfig.ComponentSettings.Base;

namespace AutoDarkModeApp
{
    public partial class DesktopBGui
    {
        readonly WallpaperHandler deskBGHandler = new WallpaperHandler();
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        bool picture1 = false;
        bool picture2 = false;
        public bool saved = false;

        public DesktopBGui()
        {
            InitializeComponent();
            Loaded += StartVoid;
        }

        private void StartVoid(object sender, RoutedEventArgs e)
        {
            List<string> monitorIds = builder.Config.WallpaperSwitch.Component.Monitors.ConvertAll(s => s.Id.Substring(s.Id.IndexOf("#"), s.Id.IndexOf("&")));
            MonitorSelectionComboBox.ItemsSource = monitorIds;
            MonitorSelectionComboBox.SelectedItem = monitorIds.FirstOrDefault();
            if (builder.Config.WallpaperSwitch.Enabled)
            {
                //do nothing??
            }
            else
            {
                SaveButton1.IsEnabled = false;
                SaveButton1.ToolTip = Properties.Resources.dbSaveToolTip;
            }
        }

        private void FilePicker1_Click(object sender, RoutedEventArgs e)
        {
            MonitorSettings settings = builder.Config.WallpaperSwitch.Component.Monitors.Find(m => m.Id.Contains((string)MonitorSelectionComboBox.SelectedItem));
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = Properties.Resources.dbPictures + "|*.png; *.jpg; *.jpeg; *.bmp",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                if (((Button)sender).CommandParameter.ToString().Equals("FilePicker1"))
                {
                    settings.LightThemeWallpaper = dlg.FileName;
                    ShowPreview(settings.LightThemeWallpaper, 1);
                }
                if (((Button)sender).CommandParameter.ToString().Equals("FilePicker2"))
                {
                    settings.DarkThemeWallpaper = dlg.FileName;
                    ShowPreview(settings.DarkThemeWallpaper, 2);
                }
            }
        }

        private void GetCurrentBG1_Click(object sender, RoutedEventArgs e)
        {
            MsgBox msgBox = new MsgBox("I'm currently broken :)", "Nope!", "Info", "close")
            {
                Owner = GetWindow(this)
            };
            msgBox.Show();
            return;
            /*MonitorSettings settings = builder.Config.WallpaperSwitch.Component.Monitors.Find(m => m.Id.Contains((string)MonitorSelectionComboBox.SelectedItem));
            if (((Button)sender).CommandParameter.ToString().Equals("GetCurrentBG1"))
            {
                //settings.LightThemeWallpaper = deskBGHandler.GetBackground();
                ShowPreview(settings.LightThemeWallpaper, 1);
            }
            if (((Button)sender).CommandParameter.ToString().Equals("GetCurrentBG2"))
            {
                //settings.DarkThemeWallpaper = deskBGHandler.GetBackground();
                ShowPreview(settings.DarkThemeWallpaper, 2);
            }*/
        }

        private void ShowPreview(string picture, int thumb)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(picture, UriKind.Absolute);
                bitmap.EndInit();

                if (thumb == 1)
                {
                    Thumb1.Source = bitmap;
                    PictureText1.Text = "";
                    picture1 = true;
                }
                if (thumb == 2)
                {
                    Thumb2.Source = bitmap;
                    PictureText2.Text = "";
                    picture2 = true;
                }
                EnableSaveButton();
            }
            catch
            {
                MsgBox msgBox = new MsgBox(Properties.Resources.dbPreviewError + Environment.NewLine + Properties.Resources.dbErrorText, Properties.Resources.errorOcurredTitle, "Wallpaper Preview Error", "close")
                {
                    Owner = GetWindow(this)
                };
                msgBox.ShowDialog();
            }
        }

        private void EnableSaveButton()
        {
            if (picture1 == true && picture2 == true)
            {
                SaveButton1.IsEnabled = true;
                SaveButton1.ToolTip = null;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                builder.Config.WallpaperSwitch.Enabled = true;
                saved = true;
                Close();
            }
            catch (Exception ex)
            {
                MsgBox msgBox = new MsgBox(Properties.Resources.dbSavedError + Environment.NewLine + Properties.Resources.dbErrorText, Properties.Resources.errorOcurredTitle + Environment.NewLine + ex, "error", "close")
                {
                    Owner = GetWindow(this)
                };
                msgBox.Show();
            }
        }

        private void DeleButton_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.WallpaperSwitch.Enabled = false;
            builder.Save();
            Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MonitorSettings settings = builder.Config.WallpaperSwitch.Component.Monitors.Find(m => m.Id.Contains((string)MonitorSelectionComboBox.SelectedItem));
            ShowPreview(settings.LightThemeWallpaper, 1);
            ShowPreview(settings.DarkThemeWallpaper, 2);
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            List<string> monitorIds = builder.Config.WallpaperSwitch.Component.Monitors.ConvertAll(s => s.Id.Substring(s.Id.IndexOf("#"), s.Id.IndexOf("&")));
            ((ComboBox)sender).ItemsSource = monitorIds;
        }
    }
}
