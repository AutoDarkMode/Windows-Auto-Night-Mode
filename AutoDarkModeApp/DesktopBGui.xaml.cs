using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AutoDarkModeSvc.Config;
using Microsoft.Win32;

namespace AutoDarkModeApp
{
    public partial class DesktopBGui
    {
        WallpaperHandler deskBGHandler = new WallpaperHandler();
        AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        string pathOrig1;
        string pathOrig2;
        string pathCur1;
        string pathCur2;
        readonly string folderPath;
        bool picture1 = false;
        bool picture2 = false;
        public bool saved = false;

        public DesktopBGui()
        {
            folderPath = Path.Combine(builder.ConfigDir, "Wallpapers");
            pathCur1 = builder.Config.Wallpaper.LightThemeWallpapers.Count > 0 ? builder.Config.Wallpaper.LightThemeWallpapers.ToList()[0] : "";
            pathCur2 = builder.Config.Wallpaper.DarkThemeWallpapers.Count > 0 ? builder.Config.Wallpaper.DarkThemeWallpapers.ToList()[0] : "";
            InitializeComponent();
            StartVoid();
        }

        private void StartVoid()
        {
            if (builder.Config.Wallpaper.Enabled)
            {
                try
                {
                    ShowPreview(pathCur1, 1);
                    ShowPreview(pathCur2, 2);
                }
                catch
                {
                    builder.Config.Wallpaper.Enabled = false;
                    StartVoid();
                }
            }
            else
            {
                CreateFolder();
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
                    pathOrig1 = dlg.FileName;
                    ShowPreview(pathOrig1, 1);
                }
                if (((Button)sender).CommandParameter.ToString().Equals("FilePicker2"))
                {
                    pathOrig2 = dlg.FileName;
                    ShowPreview(pathOrig2, 2);
                }
            }
        }

        private void GetCurrentBG1_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).CommandParameter.ToString().Equals("GetCurrentBG1"))
            {
                pathOrig1 = deskBGHandler.GetBackground();
                ShowPreview(pathOrig1, 1);
            }
            if (((Button)sender).CommandParameter.ToString().Equals("GetCurrentBG2"))
            {
                pathOrig2 = deskBGHandler.GetBackground();
                ShowPreview(pathOrig2, 2);
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

        private void CopyFileLight()
        {
            if (pathOrig1 != null)
            {
                string pathTemp1 = Path.Combine(folderPath, "WallpaperLight_Temp" + Path.GetExtension(pathOrig1));
                File.Copy(pathOrig1, pathTemp1, true);
                try
                {
                    File.Delete(pathCur1);
                }
                catch
                {

                }
                pathCur1 = Path.Combine(folderPath, "WallpaperLight" + Path.GetExtension(pathOrig1));
                File.Copy(pathTemp1, pathCur1, true);
                File.Delete(pathTemp1);
            }
        }

        private void CopyFileDark()
        {
            if(pathOrig2 != null)
            {
                string pathTemp2 = Path.Combine(folderPath, "WallpaperDark_Temp" + Path.GetExtension(pathOrig2));
                File.Copy(pathOrig2, pathTemp2, true);
                try
                {
                    File.Delete(pathCur2);
                }
                catch
                {

                }
                pathCur2 = Path.Combine(folderPath, "WallpaperDark" + Path.GetExtension(pathOrig2));
                File.Copy(pathTemp2, pathCur2, true);
                File.Delete(pathTemp2);
            }
        }

        private void CreateFolder()
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
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
                CopyFileLight();
                CopyFileDark();
                builder.Config.Wallpaper.LightThemeWallpapers.Clear();
                builder.Config.Wallpaper.DarkThemeWallpapers.Clear();
                builder.Config.Wallpaper.LightThemeWallpapers.Add(pathCur1);
                builder.Config.Wallpaper.DarkThemeWallpapers.Add(pathCur2);
                builder.Config.Wallpaper.Enabled = true;
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
            Directory.Delete(folderPath, true);
            builder.Config.Wallpaper.LightThemeWallpapers.Clear();
            builder.Config.Wallpaper.DarkThemeWallpapers.Clear();
            builder.Config.Wallpaper.Enabled = false;
            Close();
        }
    }
}
