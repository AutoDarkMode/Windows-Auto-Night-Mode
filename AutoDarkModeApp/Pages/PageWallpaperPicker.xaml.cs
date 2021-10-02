using AutoDarkModeApp.Handlers;
using AutoDarkModeComms;
using AutoDarkModeConfig;
using AutoDarkModeConfig.ComponentSettings.Base;
using AutoDarkModeSvc.Communication;
using Microsoft.Win32;
using ModernWpf.Media.Animation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageWallpaperPicker.xaml
    /// </summary>
    public partial class PageWallpaperPicker : ModernWpf.Controls.Page
    {
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private readonly ICommandClient messagingClient = new ZeroMQClient(Address.DefaultPort);
        private bool init = true;
        private bool selectedLight = true;

        public PageWallpaperPicker()
        {
            try
            {
                builder.Load();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "builder.Load at PageWallpaperPicker");
            }

            InitializeComponent();
        }

        //ui handler at start
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //is feature enabled?
            if (builder.Config.WallpaperSwitch.Enabled)
            {
                ToggleSwitchWallpaper.IsOn = true;
            }
            else
            {

            }

            //generate a list with all installed Monitors, select the first one
            List<MonitorSettings> monitorIds = builder.Config.WallpaperSwitch.Component.Monitors;
            ComboBoxMonitorSelection.ItemsSource = monitorIds;
            ComboBoxMonitorSelection.SelectedItem = monitorIds.FirstOrDefault();

            //select light mode in combobox, this will fire selection changed
            ComboBoxModeSelection.SelectedItem = ComboBoxModeSelectionLightTheme;

            init = false;
        }

        private void ToggleSwitchWallpaper_Toggled(object sender, RoutedEventArgs e)
        {
            if((sender as ModernWpf.Controls.ToggleSwitch).IsOn)
            {
                builder.Config.WallpaperSwitch.Enabled = true;
            }
            else
            {
                builder.Config.WallpaperSwitch.Enabled = false;
            }
        }

        private void ShowErrorMessage(Exception ex, string location)
        {
            string error = Properties.Resources.errorThemeApply + $"\n\nError ocurred in: {location}" + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
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

        private void ShowPreview(string picture)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(picture, UriKind.Absolute);
                bitmap.EndInit();
                ImagePreview.Source = bitmap;
                TextBlockImagePath.Text = picture;
            }
            catch  (Exception ex)
            {
                ShowErrorMessage(ex, "ShowPreview");
            }
        }

        private void ComboBoxModeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if((sender as ComboBox).SelectedItem == ComboBoxModeSelectionLightTheme)
            {
                selectedLight = true;

                switch (builder.Config.WallpaperSwitch.Component.TypeLight)
                {
                    case WallpaperType.Global:
                        ComboBoxBackgroundSelection.SelectedItem = ComboBoxBackgroundSelectionGlobal;
                        break;

                    case WallpaperType.Individual:
                        ComboBoxBackgroundSelection.SelectedItem = ComboBoxBackgroundSelectionIndividual;
                        break;

                    case WallpaperType.SolidColor:
                        ComboBoxBackgroundSelection.SelectedItem = ComboBoxBackgroundSelectionSolidColor;
                        break;
                }
            }
            else
            {
                selectedLight = false;

                switch (builder.Config.WallpaperSwitch.Component.TypeDark)
                {
                    case WallpaperType.Global:
                        ComboBoxBackgroundSelection.SelectedItem = ComboBoxBackgroundSelectionGlobal;
                        break;

                    case WallpaperType.Individual:
                        ComboBoxBackgroundSelection.SelectedItem = ComboBoxBackgroundSelectionIndividual;
                        break;

                    case WallpaperType.SolidColor:
                        ComboBoxBackgroundSelection.SelectedItem = ComboBoxBackgroundSelectionSolidColor;
                        break;
                }
            }
        }

        private void ComboBoxBackgroundSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if((sender as ComboBox).SelectedItem == ComboBoxBackgroundSelectionGlobal)
            {
                ComboBoxMonitorSelection.Visibility = Visibility.Collapsed;

                if (selectedLight)
                {
                    if(builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light != null)
                    {
                        ShowPreview(builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light);
                    }
                }
                else
                {
                    if (builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark != null)
                    {
                        ShowPreview(builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark);
                    }
                }
            }

            if((sender as ComboBox).SelectedItem == ComboBoxBackgroundSelectionIndividual)
            {
                ComboBoxMonitorSelection.Visibility = Visibility.Visible;
            }

            if((sender as ComboBox).SelectedItem == ComboBoxBackgroundSelectionSolidColor)
            {
                ComboBoxMonitorSelection.Visibility = Visibility.Collapsed;
            }
        }

        private void ComboBoxMonitorSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MonitorSettings monitorSettings = (MonitorSettings)ComboBoxMonitorSelection.SelectedItem;
            if (selectedLight)
            {
                ShowPreview(monitorSettings.LightThemeWallpaper);
            }
            else
            {
                ShowPreview(monitorSettings.DarkThemeWallpaper);
            }
        }

        private void ButtonFilePicker_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = Properties.Resources.dbPictures + "|*.png; *.jpg; *.jpeg; *.bmp",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            bool? result = ofd.ShowDialog();
            if (result == true)
            {
                SetWallpaper(ofd.FileName);
                ShowPreview(ofd.FileName);
            }
        }

        private void SetWallpaper(string FileName)
        {
            if(ComboBoxBackgroundSelection.SelectedItem == ComboBoxBackgroundSelectionGlobal)
            {
                if (selectedLight)
                {
                    builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light = FileName;
                    builder.Config.WallpaperSwitch.Component.TypeLight = WallpaperType.Global;
                }
                else
                {
                    builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark = FileName;
                    builder.Config.WallpaperSwitch.Component.TypeDark = WallpaperType.Global;
                }
            }

            if(ComboBoxBackgroundSelection.SelectedItem == ComboBoxBackgroundSelectionIndividual)
            {
                MonitorSettings monitorSettings = (MonitorSettings)ComboBoxMonitorSelection.SelectedItem;
                if (selectedLight)
                {
                    monitorSettings.LightThemeWallpaper = FileName;
                    builder.Config.WallpaperSwitch.Component.TypeLight = WallpaperType.Individual;
                }
                else
                {
                    monitorSettings.DarkThemeWallpaper = FileName;
                    builder.Config.WallpaperSwitch.Component.TypeDark = WallpaperType.Individual;
                }
            }

            builder.Save();
            RequestThemeSwitch();
        }

        private async void RequestThemeSwitch()
        {
            try
            {
                string result = await messagingClient.SendMessageAndGetReplyAsync(Command.Switch, 15);
                if (result != StatusCode.Ok)
                {
                    throw new SwitchThemeException(result, "PageApps");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "RequestThemeSwitch at PageWallpaperPicker");
            }
        }
    }
}
