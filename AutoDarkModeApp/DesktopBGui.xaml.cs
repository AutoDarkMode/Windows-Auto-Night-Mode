using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AutoDarkModeApp.Config;
using Microsoft.Win32;

namespace AutoDarkModeApp
{
    public partial class DesktopBGui
    {
        DeskBGHandler deskBGHandler = new DeskBGHandler();
        private readonly AutoDarkModeConfigBuilder configBuilder = AutoDarkModeConfigBuilder.Instance();
        string pathLight;
        string pathDark;
        bool picture1 = false;
        bool picture2 = false;
        public bool saved = false;

        public DesktopBGui()
        {
            //
            // The following bad code will be implemented more efficient and easy to read
            //
            List<string> lightThemeWallpapers = (List<string>)configBuilder.Config.Wallpaper.LightThemeWallpapers;
            List<string> darkThemeWallpapers = (List<string>)configBuilder.Config.Wallpaper.DarkThemeWallpapers;

            if (lightThemeWallpapers.Count != 0)
            {
                pathLight = ((List<string>)configBuilder.Config.Wallpaper.LightThemeWallpapers)[0];
            }

            if (darkThemeWallpapers.Count != 0)
            {
                pathDark = ((List<string>)configBuilder.Config.Wallpaper.DarkThemeWallpapers)[0];
            }
            

            InitializeComponent();
            StartVoid();
        }

        private void StartVoid()
        {
            if(configBuilder.Config.Wallpaper.Enabled)
            //if (Properties.Settings.Default.WallpaperSwitch == true)
            {
                try
                {
                    ShowPreview(pathLight, 1);
                    ShowPreview(pathDark, 2);
                }
                catch
                {
                    configBuilder.Config.Wallpaper.Enabled = false;
                    Properties.Settings.Default.WallpaperSwitch = false;
                    StartVoid();
                }
            }
            else
            {
                SaveButton1.IsEnabled = false;
                SaveButton1.ToolTip = Properties.Resources.dbSaveToolTip;
            }
        }

        private void FilePicker1_Click(object sender, RoutedEventArgs e)
        {
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
                    pathLight = dlg.FileName;
                    ShowPreview(pathLight, 1);
                }
                if (((Button)sender).CommandParameter.ToString().Equals("FilePicker2"))
                {
                    pathDark = dlg.FileName;
                    ShowPreview(pathDark, 2);
                }
            }
        }

        private void GetCurrentBG1_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).CommandParameter.ToString().Equals("GetCurrentBG1"))
            {
                pathLight = deskBGHandler.GetBackground();
                ShowPreview(pathLight, 1);
            }
            if (((Button)sender).CommandParameter.ToString().Equals("GetCurrentBG2"))
            {
                pathDark = deskBGHandler.GetBackground();
                ShowPreview(pathDark, 2);
            }
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
                MsgBox msgBox = new MsgBox(Properties.Resources.dbPreviewError + Environment.NewLine + Properties.Resources.dbErrorText, Properties.Resources.errorOcurredTitle, "error", "close")
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
                configBuilder.Config.Wallpaper.Enabled = true;
                configBuilder.Config.Wallpaper.LightThemeWallpapers.Clear();
                configBuilder.Config.Wallpaper.DarkThemeWallpapers.Clear();
                configBuilder.Config.Wallpaper.LightThemeWallpapers.Add(pathLight);
                configBuilder.Config.Wallpaper.DarkThemeWallpapers.Add(pathDark);
                Properties.Settings.Default.WallpaperSwitch = true;
                configBuilder.Save();
                saved = true;
                Close();
            }
            catch
            {
                MsgBox msgBox = new MsgBox(Properties.Resources.dbSavedError + Environment.NewLine + Properties.Resources.dbErrorText, Properties.Resources.errorOcurredTitle, "error", "close")
                {
                    Owner = GetWindow(this)
                };
                msgBox.Show();
            }
        }

        private void DeleButton_Click(object sender, RoutedEventArgs e)
        {
            configBuilder.Config.Wallpaper.Enabled = false;
            configBuilder.Config.Wallpaper.LightThemeWallpapers.Clear();
            configBuilder.Config.Wallpaper.DarkThemeWallpapers.Clear();
            configBuilder.Save();
            Properties.Settings.Default.WallpaperSwitch = false;            
            Close();
        }
    }
}
