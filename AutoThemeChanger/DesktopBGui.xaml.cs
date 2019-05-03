using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for DesktopBGui.xaml
    /// </summary>
    public partial class DesktopBGui : Window
    {
        DeskBGHandler deskBGHandler = new DeskBGHandler();
        string pathOrig1;
        string pathOrig2;
        string pathCur1 = Properties.Settings.Default.WallpaperLight;
        string pathCur2 = Properties.Settings.Default.WallpaperDark;
        readonly string folderPath = "Wallpaper/";
        bool picture1 = false;
        bool picture2 = false;
        bool folderExist;

        public DesktopBGui()
        {
            InitializeComponent();
            CreateFolder();
            if(pathCur1 != "" && pathCur2 != "" & folderExist  == true)
            {
                ShowPreview(pathCur1, 1);
                ShowPreview(pathCur2, 2); 
            }
            else
            {
                SaveButton1.IsEnabled = false;
                SaveButton1.ToolTip = "Please select two Wallpaper";
            }
        }

        private void FilePicker1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Pictures |*.png; *.jpg; *jpeg";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
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
                PictureText1.Text = "";
                picture1 = true;
            }
            if (((Button)sender).CommandParameter.ToString().Equals("GetCurrentBG2"))
            {
                pathOrig2 = deskBGHandler.GetBackground();
                ShowPreview(pathOrig2, 2);
                PictureText2.Text = "";
                picture2 = true;
            }
            EnableSaveButton();
        }

        private void ShowPreview(string picture, int thumb)
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

        private void CopyFileLight()
        {
            try
            {
                File.Delete(pathCur1);
            }
            catch
            {

            }
            pathCur1 = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/" + folderPath + "WallpaperLight" + Path.GetExtension(pathOrig1);
            File.Copy(pathOrig1, pathCur1, true);
        }

        private void CopyFileDark()
        {
            try
            {
                File.Delete(pathCur2);
            }
            catch
            {

            }
            pathCur2 = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/" + folderPath + "WallpaperDark" + Path.GetExtension(pathOrig2);
            File.Copy(pathOrig2, pathCur2, true);
        }

        private void CreateFolder()
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                folderExist = false;
            }
            else
            {
                folderExist = true;
            }
        }

        private void EnableSaveButton()
        {
            if(picture1 == true && picture2 == true)
            {
                SaveButton1.IsEnabled = true;
                SaveButton1.ToolTip = null;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveButton1_Click(object sender, RoutedEventArgs e)
        {
            CopyFileLight();
            CopyFileDark();
            Properties.Settings.Default.WallpaperLight = pathCur1;
            Properties.Settings.Default.WallpaperDark = pathCur2;
            Properties.Settings.Default.WallpaperSwitch = true;
            this.Close();
        }

        private void DeleButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.Delete(folderPath, true);
            Properties.Settings.Default.WallpaperLight = "";
            Properties.Settings.Default.WallpaperDark = "";
            Properties.Settings.Default.WallpaperSwitch = false;
            this.Close();
        }
    }
}
