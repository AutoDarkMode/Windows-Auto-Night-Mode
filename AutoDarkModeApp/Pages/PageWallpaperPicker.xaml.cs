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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private bool SelectedLight { get; set; } = true;
        private delegate void ShowPreviewDelegate(string picture);

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
            if ((sender as ModernWpf.Controls.ToggleSwitch).IsOn)
            {
                builder.Config.WallpaperSwitch.Enabled = true;
            }
            else
            {
                builder.Config.WallpaperSwitch.Enabled = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "PageWallpaperPicker");
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
            ImagePreview.Visibility = Visibility.Collapsed;
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(picture, UriKind.Absolute);
                bitmap.EndInit();
                ImagePreview.Source = bitmap;
                TextBlockImagePath.Text = picture;
                ImagePreview.Visibility = Visibility.Visible;
                TextBlockImagePath.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "ShowPreview");
            }
        }

        private void ComboBoxModeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox).SelectedItem == ComboBoxModeSelectionLightTheme)
            {
                SelectedLight = true;

                switch (builder.Config.WallpaperSwitch.Component.TypeLight)
                {
                    case WallpaperType.Global:
                        ComboBoxWallpaperTypeSelection.SelectedItem = ComboBoxBackgroundSelectionGlobal;
                        GridWallpaper.Visibility = Visibility.Visible;
                        SolidColorPicker.Visibility = Visibility.Collapsed;
                        break;

                    case WallpaperType.Individual:
                        GridWallpaper.Visibility = Visibility.Visible;
                        SolidColorPicker.Visibility = Visibility.Collapsed;
                        ComboBoxWallpaperTypeSelection.SelectedItem = ComboBoxBackgroundSelectionIndividual;
                        break;

                    case WallpaperType.SolidColor:
                        GridWallpaper.Visibility = Visibility.Collapsed;
                        SolidColorPicker.Visibility = Visibility.Visible;
                        ComboBoxWallpaperTypeSelection.SelectedItem = ComboBoxBackgroundSelectionSolidColor;
                        break;
                }
            }
            else
            {
                SelectedLight = false;

                switch (builder.Config.WallpaperSwitch.Component.TypeDark)
                {
                    case WallpaperType.Global:
                        ComboBoxWallpaperTypeSelection.SelectedItem = ComboBoxBackgroundSelectionGlobal;
                        break;

                    case WallpaperType.Individual:
                        ComboBoxWallpaperTypeSelection.SelectedItem = ComboBoxBackgroundSelectionIndividual;
                        break;

                    case WallpaperType.SolidColor:
                        ComboBoxWallpaperTypeSelection.SelectedItem = ComboBoxBackgroundSelectionSolidColor;
                        break;
                }
            }
            ComboBoxWallpaperTypeSelection_SelectionChanged(ComboBoxWallpaperTypeSelection, new NoSaveEvent(true));
        }

        private void ComboBoxWallpaperTypeSelection_SelectionChanged(object sender, EventArgs e)
        {
            if (sender is ComboBox && (sender as ComboBox).SelectedItem == ComboBoxBackgroundSelectionGlobal)
            {
                HandleSelectionGlobal();
            }
            else if ((sender as ComboBox).SelectedItem == ComboBoxBackgroundSelectionIndividual)
            {
                GridWallpaper.Visibility = Visibility.Visible;
                SolidColorPicker.Visibility = Visibility.Collapsed;
                ComboBoxMonitorSelection_SelectionChanged(this, null);
                ComboBoxMonitorSelection.Visibility = Visibility.Visible;
            }
            else if ((sender as ComboBox).SelectedItem == ComboBoxBackgroundSelectionSolidColor)
            {
                ComboBoxMonitorSelection.Visibility = Visibility.Collapsed;
                GridWallpaper.Visibility = Visibility.Collapsed;
                SolidColorPicker.Visibility = Visibility.Visible;
                if (ComboBoxModeSelection.SelectedItem == ComboBoxModeSelectionLightTheme)
                {
                    HexColorTextBox.Text = builder.Config.WallpaperSwitch.Component.SolidColors.Light;
                    try
                    {
                        ColorPreview.Fill = new SolidColorBrush(HexToColor(HexColorTextBox.Text));
                    }
                    catch { }
                }
                else
                {
                    HexColorTextBox.Text = builder.Config.WallpaperSwitch.Component.SolidColors.Dark;
                    try
                    {
                        ColorPreview.Fill = new SolidColorBrush(HexToColor(HexColorTextBox.Text));
                    }
                    catch { }
                }

            }
            if (e is NoSaveEvent nse)
            {
                SaveWallpaperTypeSelection(nse.NoSave);
            }
            SaveWallpaperTypeSelection(false);
        }

        private void HandleSelectionGlobal()
        {
            ComboBoxMonitorSelection.Visibility = Visibility.Collapsed;
            SolidColorPicker.Visibility = Visibility.Collapsed;
            GridWallpaper.Visibility = Visibility.Visible;

            if (SelectedLight)
            {
                if (builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light != null)
                {
                    ShowPreview(builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light);
                }
                else
                {
                    ImagePreview.Visibility = Visibility.Collapsed;
                    TextBlockImagePath.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark != null)
                {
                    ShowPreview(builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark);
                }
                else
                {
                    ImagePreview.Visibility = Visibility.Collapsed;
                    TextBlockImagePath.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SaveWallpaperTypeSelection(bool noSave)
        {
            // only save if sender is own combobox
            if (noSave || init)
            {
                return;
            }
            if (ComboBoxModeSelection.SelectedItem == ComboBoxModeSelectionLightTheme)
            {
                builder.Config.WallpaperSwitch.Component.TypeLight = WallpaperTypeTextToType(ComboBoxWallpaperTypeSelection.SelectedItem as ComboBoxItem);
            }
            else if (ComboBoxModeSelection.SelectedItem == ComboBoxModeSelectionDarkTheme)
            {
                builder.Config.WallpaperSwitch.Component.TypeDark = WallpaperTypeTextToType(ComboBoxWallpaperTypeSelection.SelectedItem as ComboBoxItem);
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "PageWallpaperPicker");
            }
        }

        private WallpaperType WallpaperTypeTextToType(ComboBoxItem item)
        {
            if (item == ComboBoxBackgroundSelectionGlobal)
            {
                return WallpaperType.Global;
            }
            else if (item == ComboBoxBackgroundSelectionIndividual)
            {
                return WallpaperType.Individual;
            }
            else if (item == ComboBoxBackgroundSelectionSolidColor)
            {
                return WallpaperType.SolidColor;
            }
            return WallpaperType.Unknown;
        }

        private void ComboBoxMonitorSelection_SelectionChanged(object sender, EventArgs e)
        {
            MonitorSettings monitorSettings = (MonitorSettings)ComboBoxMonitorSelection.SelectedItem;
            if (SelectedLight)
            {
                Dispatcher.BeginInvoke(new ShowPreviewDelegate(ShowPreview), monitorSettings.LightThemeWallpaper);
            }
            else
            {
                Dispatcher.BeginInvoke(new ShowPreviewDelegate(ShowPreview), monitorSettings.DarkThemeWallpaper);
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
            if (ComboBoxWallpaperTypeSelection.SelectedItem == ComboBoxBackgroundSelectionGlobal)
            {
                if (SelectedLight)
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

            if (ComboBoxWallpaperTypeSelection.SelectedItem == ComboBoxBackgroundSelectionIndividual)
            {
                MonitorSettings monitorSettings = (MonitorSettings)ComboBoxMonitorSelection.SelectedItem;
                if (SelectedLight)
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

        private void TextBlockBackButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Frame.Navigate(typeof(PageWallpaper), null, new DrillInNavigationTransitionInfo());
        }

        private void HexColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string prefix = "#";
            if (!HexColorTextBox.Text.StartsWith(prefix))
            {
                HexColorTextBox.Text = prefix + HexColorTextBox.Text;
                HexColorTextBox.SelectionStart = HexColorTextBox.Text.Length;
            }
            try
            {
                ColorPreview.Fill = new SolidColorBrush(HexToColor(HexColorTextBox.Text));
            }
            catch { }
        }

        public static Color HexToColor(string hexString)
        {
            if (hexString.IndexOf('#') != -1)
                hexString = hexString.Replace("#", "");

            int r = int.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            int g = int.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            int b = int.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        }

        protected static bool CheckValidFormatHtmlColor(string inputColor)
        {
            //regex from http://stackoverflow.com/a/1636354/2343
            if (Regex.Match(inputColor, "^#(?:[0-9a-fA-F]{3}){1,2}$").Success)
                return true;

            var result = System.Drawing.Color.FromName(inputColor);
            return result.IsKnownColor;
        }

        private void ButtonColorSet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckValidFormatHtmlColor(HexColorTextBox.Text))
                {
                    if (ComboBoxModeSelection.SelectedItem == ComboBoxModeSelectionLightTheme)
                    {
                        builder.Config.WallpaperSwitch.Component.SolidColors.Light = HexColorTextBox.Text;
                    }
                    else
                    {
                        builder.Config.WallpaperSwitch.Component.SolidColors.Dark = HexColorTextBox.Text;
                    }
                    builder.Save();
                }
                else
                {
                    ShowErrorMessage(new FormatException("invalid hex string"), "buttoncolorset");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "buttoncolorset");
            }
        }
    }

    class NoSaveEvent : EventArgs
    {
        public NoSaveEvent(bool noSave)
        {
            NoSave = noSave;
        }
        public bool NoSave { get; private set; }
    }
}
