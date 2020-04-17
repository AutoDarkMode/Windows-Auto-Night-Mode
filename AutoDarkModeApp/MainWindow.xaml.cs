using AutoDarkMode;
using AutoDarkModeApp.Communication;
using AutoDarkModeSvc.Config;
using AutoDarkModeApp.Handlers;
using AutoDarkModeSvc.Handlers;
using NetMQ;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Windows.System.Power;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace AutoDarkModeApp
{
    public partial class MainWindow
    {
        private readonly RegeditHandler regEditHandler = new RegeditHandler();
        private readonly AutoDarkModeConfigBuilder configBuilder = AutoDarkModeConfigBuilder.Instance();
        private ICommandClient CommandClient { get; }
        private bool isClosed = false;
        private bool initComplete = false;

        public MainWindow()
        {
            CommandClient = new ZeroMQClient(Command.DefaultPort);
            LanguageHelper();
            LoadConfig();
            InitializeComponent();
            ConfigureComponents();
            if (Properties.Settings.Default.FirstRun)
            {
                AddJumpList();
                Properties.Settings.Default.FirstRun = false;
            }
        }

        #region UIHandlers
        // Window Handlers
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Updater updater = new Updater();
            updater.CheckNewVersion();
            LanguageHelper();
            DonationScreen();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            SaveConfig();
            if (App.Mutex.WaitOne(TimeSpan.FromMilliseconds(100)))
            {
                App.Mutex.ReleaseMutex();
            }
            App.Mutex.Dispose();
            Application.Current.Shutdown();
            // workaround to counter async running clients while context is being closed!
            CommandClient.SendMessage("frontend shutdown");
            Process.GetCurrentProcess().Kill();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (configBuilder.Config.ClassicMode) CommandClient.SendMessage(Command.Shutdown);
            isClosed = true;
            base.OnClosing(e);
        }

        // Textbox Handlers
        private void TextBox_BlockChars_TextInput(object sender, TextCompositionEventArgs e)
        {
            applyButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_BlockChars_TextInput_Offset(object sender, TextCompositionEventArgs e)
        {
            OffsetButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_BlockCopyPaste_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void TexttBox_SelectAll_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var textBox = ((System.Windows.Controls.TextBox)sender);
            textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.SelectAll();
            }));
        }
        private void TextBox_TabNext_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (((TextBox)sender).MaxLength == ((TextBox)sender).Text.Length)
            {
                var ue = e.OriginalSource as FrameworkElement;
                e.Handled = true;
                ue.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        // Button Handlers
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow
            {
                Owner = GetWindow(this)
            };
            aboutWindow.ShowDialog();
            // todo: this is fine, can stay like this
            if (aboutWindow.BckgrUpdateCB.IsChecked == true && Properties.Settings.Default.BackgroundUpdate == false)
            {
                TaskSchdHandler.CreateAppUpdaterTask();
                Properties.Settings.Default.BackgroundUpdate = true;
            }
            else if (aboutWindow.BckgrUpdateCB.IsChecked == false && Properties.Settings.Default.BackgroundUpdate == true)
            {
                TaskSchdHandler.RemoveAppUpdaterTask();
                Properties.Settings.Default.BackgroundUpdate = false;
            }
        }
        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int darkStart;
            int darkStartMinutes;
            int lightStart;
            int lightStartMinutes;

            string selectedLightTheme = (string)LightThemeComboBox.SelectedItem;
            configBuilder.Config.LightThemePath = GetUserThemes().Where(t => t.Contains(selectedLightTheme)).FirstOrDefault();

            string selectedDarkTheme = (string)DarkThemeComboBox.SelectedItem;
            configBuilder.Config.DarkThemePath = GetUserThemes().Where(t => t.Contains(selectedDarkTheme)).FirstOrDefault();

            //get values from TextBox
            try
            {
                darkStart = int.Parse(DarkStartHoursBox.Text);
                darkStartMinutes = int.Parse(DarkStartMinutesBox.Text);
                lightStart = int.Parse(LightStartHoursBox.Text);
                lightStartMinutes = int.Parse(LightStartMinutesBox.Text);
            }
            catch
            {
                userFeedback.Text = Properties.Resources.errorNumberInput;
                return;
            }

            //check values from TextBox
            if (darkStart >= 24)
            {
                darkStart = 23;
                darkStartMinutes = 59;
            }
            if (lightStart >= darkStart)
            {
                lightStart = darkStart - 3;
            }
            if (lightStart < 0)
            {
                lightStart = 6;
                darkStart = 17;
            }

            if (lightStartMinutes > 59)
            {
                lightStartMinutes = 59;
            }
            if (darkStartMinutes > 59)
            {
                darkStartMinutes = 59;
            }
            DarkStartHoursBox.Text = Convert.ToString(darkStart);
            LightStartHoursBox.Text = Convert.ToString(lightStart);
            if (lightStartMinutes < 10)
            {
                LightStartMinutesBox.Text = "0" + Convert.ToString(lightStartMinutes);
            }
            else
            {
                LightStartMinutesBox.Text = Convert.ToString(lightStartMinutes);
            }
            if (darkStartMinutes < 10)
            {
                DarkStartMinutesBox.Text = "0" + Convert.ToString(darkStartMinutes);
            }
            else
            {
                DarkStartMinutesBox.Text = Convert.ToString(darkStartMinutes);
            }
            if (!configBuilder.Config.Location.Enabled)
            {
                configBuilder.Config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, lightStart, lightStartMinutes, 0);
                configBuilder.Config.Sunset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, darkStart, darkStartMinutes, 0);
            }

            try
            {
                if (initComplete)
                {
                    configBuilder.Save();
                }
                bool isMessageOK = await CommandClient.SendMessageAsync(Command.Switch);
                if (!isMessageOK)
                {
                    throw new SwitchThemeException();
                }
                else
                {
                    applyButton.IsEnabled = false;
                    if (PowerManager.EnergySaverStatus == EnergySaverStatus.On)
                    {
                        userFeedback.Text = Properties.Resources.msgChangesSaved + "\n\n" + Properties.Resources.msgBatterySaver;
                        applyButton.IsEnabled = true;
                    }
                    else
                    {
                        userFeedback.Text = Properties.Resources.msgChangesSaved; // changes were saved!
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }
        private void OffsetModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Content.ToString() == "+")
                {
                    button.Content = "-";
                }
                else
                {
                    button.Content = "+";
                }
                OffsetButton.IsEnabled = true;
            }
        }
        private void OffsetButton_Click(object sender, RoutedEventArgs e)
        {
            int offsetDark;
            int offsetLight;

            //get values from TextBox
            try
            {
                offsetDark = int.Parse(OffsetDarkBox.Text);
                offsetLight = int.Parse(OffsetLightBox.Text);
            }
            catch
            {
                userFeedback.Text = Properties.Resources.errorNumberInput;
                return;
            }

            PopulateOffsetFields(offsetDark, offsetLight);

            if (OffsetLightModeButton.Content.ToString() == "+")
            {
                configBuilder.Config.Location.SunriseOffsetMin = offsetLight;
            }
            else
            {
                configBuilder.Config.Location.SunriseOffsetMin = -offsetLight;
            }

            if (OffsetDarkModeButton.Content.ToString() == "+")
            {
                configBuilder.Config.Location.SunsetOffsetMin = offsetDark;
            }
            else
            {
                configBuilder.Config.Location.SunsetOffsetMin = -offsetDark;
            }

            configBuilder.Save();
            OffsetButton.IsEnabled = false;
            GetLocation();
        }
        private void BGWinButton_Click(object sender, RoutedEventArgs e)
        {
            DesktopBGui BGui = new DesktopBGui
            {
                Owner = GetWindow(this)
            };
            BGui.ShowDialog();
            if (BGui.saved == true)
            {
                ApplyButton_Click(applyButton, null);
            }
            SetDesktopBackgroundStatus();
        }

        // CheckBox Handlers
        private void LocationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // todo: info for utku, I've already partly implemented this
            // to use the command pipe infrastructure to test if the backend works.
            // this also serves as an example how to use the new command infrastructure
            // for UI operations use the Async variant to prevent UI blocking
            if (initComplete)
            {
                configBuilder.Config.Location.Enabled = true;
                configBuilder.Save();
            }
            GetLocation();
        }
        private void LocationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                LightStartHoursBox.IsEnabled = true;
                LightStartMinutesBox.IsEnabled = true;
                DarkStartHoursBox.IsEnabled = true;
                DarkStartMinutesBox.IsEnabled = true;
                applyButton.IsEnabled = true;
                locationBlock.Visibility = Visibility.Collapsed;
                SetOffsetVisibility(Visibility.Collapsed);
                configBuilder.Config.Location.Enabled = false;
                configBuilder.Save();

            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            userFeedback.Text = Properties.Resources.msgClickApply; // Click on apply to save changes
        }
        private async void AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            configBuilder.Config.AutoThemeSwitchingEnabled = true;
            LightThemeComboBox.IsEnabled = true;
            locationCheckBox.IsEnabled = true;
            applyButton.IsEnabled = true;
            DarkStartHoursBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            LightStartHoursBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            BGWinButton.IsEnabled = true;
            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes

            try
            {
                configBuilder.Save();
                bool isMessageOk = await CommandClient.SendMessageAsync(Command.AddAutostart);
                if (!isMessageOk)
                {
                    throw new AddAutoStartException();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }
        private async void AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            configBuilder.Config.AutoThemeSwitchingEnabled = false;
            configBuilder.Config.Wallpaper.Enabled = false;
            DarkThemeComboBox.IsEnabled = false;
            LightThemeComboBox.IsEnabled = false;
            //.IsEnabled = false;
            locationCheckBox.IsEnabled = false;
            locationCheckBox.IsChecked = false;
            applyButton.IsEnabled = false;
            DarkStartHoursBox.IsEnabled = false;
            DarkStartMinutesBox.IsEnabled = false;
            LightStartHoursBox.IsEnabled = false;
            LightStartMinutesBox.IsEnabled = false;
            BGWinButton.IsEnabled = false;
            userFeedback.Text = Properties.Resources.welcomeText; //Activate the checkbox to enable automatic theme switching
            SetDesktopBackgroundStatus();
            try
            {
                configBuilder.Save();
                bool isMessageOk = await CommandClient.SendMessageAsync(Command.RemoveAutostart);
                if (!isMessageOk)
                {
                    throw new RemoveAutoStartException();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }
        private async void AccentColorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            configBuilder.Config.AccentColorTaskbarEnabled = true;
            try
            {
                configBuilder.Save();
                bool isMessageOk = await CommandClient.SendMessageAsync(Command.Switch);
                if (!isMessageOk)
                {
                    throw new SwitchThemeException();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }
        private async void AccentColorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            configBuilder.Config.AccentColorTaskbarEnabled = false;
            try
            {
                configBuilder.Save();
                bool isMessageOk = await CommandClient.SendMessageAsync(Command.Switch);
                if (!isMessageOk)
                {
                    throw new SwitchThemeException();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        // ComboBox Handlers
        private void LightThemeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            applyButton.IsEnabled = true;
        }
        private void DarkThemeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            applyButton.IsEnabled = true;
        }
        #endregion

        private void DonationScreen()
        {
            Random rdmnumber = new Random();
            int generatedNumber = rdmnumber.Next(1, 100);
            if (generatedNumber == 50)
            {
                MsgBox msgBox = new MsgBox(Properties.Resources.donationDescription, Properties.Resources.donationTitle, "smiley", "yesno");
                msgBox.Owner = GetWindow(this);
                msgBox.ShowDialog();
                var result = msgBox.DialogResult;
                if (result == true)
                {
                    string donateUri = @"https://www.paypal.me/arminosaj";
                    Process.Start(new ProcessStartInfo(donateUri)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
        }
        private void LanguageHelper()
        {
            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.Language.ToString()))
            {
                Properties.Settings.Default.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToString();
            }
            CultureInfo.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Language, true);
        }
        private void ConfigureComponents()
        {
            if (configBuilder.Config.AutoThemeSwitchingEnabled)
            {
                autoCheckBox.IsChecked = true;
                if (configBuilder.Config.Location.Enabled)
                {
                    locationCheckBox.IsChecked = true;
                    DateTime Sunset = configBuilder.Config.Sunset;
                    DateTime Sunrise = configBuilder.Config.Sunrise;
                    Sunrise.AddMinutes(configBuilder.Config.Location.SunriseOffsetMin);
                    Sunset.AddMinutes(configBuilder.Config.Location.SunsetOffsetMin);

                    DarkStartHoursBox.Text = Convert.ToString(configBuilder.Config.Sunset.Hour);
                    DarkStartMinutesBox.Text = Convert.ToString(configBuilder.Config.Sunset.Minute);
                    LightStartHoursBox.Text = Convert.ToString(configBuilder.Config.Sunrise.Hour);
                    LightStartMinutesBox.Text = Convert.ToString(configBuilder.Config.Sunrise.Minute);
                }
            }
            else
            {
                AutoCheckBox_Unchecked(autoCheckBox, null);
            }

            var themePaths = GetUserThemes();
            var themeNames = themePaths.Select(s => Path.GetFileNameWithoutExtension(s));
            LightThemeComboBox.ItemsSource = themeNames;
            DarkThemeComboBox.ItemsSource = themeNames;


            if (configBuilder.Config.DarkThemePath != null)
            {
                DarkThemeComboBox.SelectedItem = Path.GetFileNameWithoutExtension(configBuilder.Config.DarkThemePath);
            }
            if (configBuilder.Config.LightThemePath != null)
            {
                LightThemeComboBox.SelectedItem = Path.GetFileNameWithoutExtension(configBuilder.Config.LightThemePath);
            }

            SetDesktopBackgroundStatus();
            PopulateOffsetFields(configBuilder.Config.Location.SunsetOffsetMin, configBuilder.Config.Location.SunriseOffsetMin);
            initComplete = true;
        }
        private void SetDesktopBackgroundStatus()
        {
            if (!configBuilder.Config.Wallpaper.Enabled)
            {
                DeskBGStatus.Text = Properties.Resources.disabled;
            }
            else
            {
                DeskBGStatus.Text = Properties.Resources.enabled;
            }
        }
        private void PopulateOffsetFields(int offsetDark, int offsetLight)
        {
            if (offsetLight < 0)
            {
                OffsetLightModeButton.Content = "-";
                OffsetLightBox.Text = Convert.ToString(-offsetLight);
            }
            else
            {
                OffsetLightBox.Text = Convert.ToString(offsetLight);
            }
            if (offsetDark < 0)
            {
                OffsetDarkModeButton.Content = "-";
                OffsetDarkBox.Text = Convert.ToString(-offsetDark);
            }
            else
            {
                OffsetDarkBox.Text = Convert.ToString(offsetDark);
            }
        }
        public async void GetLocation()
        {
            SetOffsetVisibility(Visibility.Visible);
            locationBlock.Visibility = Visibility.Visible;
            locationBlock.Text = Properties.Resources.msgSearchLoc; // Searching your location...
            LocationHandler locationHandler = new LocationHandler();
            //invoking the location command will always enable location services by default

            var accessStatus = Command.Ok;
            if (initComplete)
            {
                accessStatus = await CommandClient.SendMesssageAndGetReplyAsync(Command.Location);
            }
            configBuilder.Load();
            if (accessStatus != Command.NoLocAccess)
            {
                //locate user + get sunrise & sunset times
                locationBlock.Text = Properties.Resources.lblCity + ": " + await locationHandler.GetCityName();
                int[] sundate = locationHandler.CalculateSunTime(false);

                //apply settings & change UI
                LightStartHoursBox.Text = sundate[0].ToString();
                LightStartMinutesBox.Text = sundate[1].ToString();
                DarkStartHoursBox.Text = sundate[2].ToString();

                DarkStartMinutesBox.Text = sundate[3].ToString();
                LightStartHoursBox.IsEnabled = false;
                LightStartMinutesBox.IsEnabled = false;
                DarkStartHoursBox.IsEnabled = false;
                DarkStartMinutesBox.IsEnabled = false;
                applyButton.IsEnabled = false;
                ApplyButton_Click(applyButton, null);
            }
            else
            {
                NoLocationAccess();
            }
        }
        private async void NoLocationAccess()
        {
            configBuilder.Config.Location.Enabled = false;
            locationCheckBox.IsChecked = false;
            locationBlock.Text = Properties.Resources.msgLocPerm; // The App needs permission to location
            locationBlock.Visibility = Visibility.Visible;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }

        private List<string> GetUserThemes()
        {
            string themeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes";
            return Directory.EnumerateFiles(themeDirectory, "*.theme", SearchOption.TopDirectoryOnly).ToList();
        }

        private void AddJumpList()
        {
            JumpTask darkJumpTask = new JumpTask
            {
                Title = Properties.Resources.lblDarkTheme,//Dark theme
                Arguments = "/dark",
                CustomCategory = Properties.Resources.lblSwitchTheme//Switch current theme
            };
            JumpTask lightJumpTask = new JumpTask
            {
                Title = Properties.Resources.lblLightTheme,//Light theme
                Arguments = "/light",
                CustomCategory = Properties.Resources.lblSwitchTheme//Switch current theme
            };

            JumpList jumpList = new JumpList();
            jumpList.JumpItems.Add(darkJumpTask);
            jumpList.JumpItems.Add(lightJumpTask);
            jumpList.ShowFrequentCategory = false;
            jumpList.ShowRecentCategory = false;
            JumpList.SetJumpList(Application.Current, jumpList);
        }
        private void SetOffsetVisibility(Visibility value)
        {
            OffsetLbl.Visibility = value;
            OffsetDarkLbl.Visibility = value;
            OffsetDarkModeButton.Visibility = value;
            OffsetLightLbl.Visibility = value;
            OffsetLightModeButton.Visibility = value;
            OffsetLightBox.Visibility = value;
            OffsetDarkBox.Visibility = value;
            OffsetDarkDot.Visibility = value;
            OffsetLightDot.Visibility = value;
            OffsetButton.Visibility = value;
        }
        private void ShowErrorMessage(Exception ex)
        {
            if (isClosed)
            {
                return;
            }
            userFeedback.Text = Properties.Resources.msgErrorOcc;
            string error = Properties.Resources.errorThemeApply + "\n\nError ocurred in: " + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = GetWindow(this)
            };
            msg.ShowDialog();
            var result = msg.DialogResult;
            if (result == true)
            {
                string issueUri = @"https://github.com/Armin2208/Windows-Auto-Night-Mode/issues";
                Process.Start(new ProcessStartInfo(issueUri) { 
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            return;
        }
        private void SaveConfig()
        {
            try
            {
                configBuilder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }
        private void LoadConfig()
        {
            try
            {
                configBuilder.Load();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }
    }
}