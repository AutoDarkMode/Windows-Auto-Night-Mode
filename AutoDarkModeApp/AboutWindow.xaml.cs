using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using AutoDarkModeSvc;
using AutoDarkModeSvc.Config;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace AutoDarkModeApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AboutWindow
    {
        private readonly AutoDarkModeConfigBuilder configBuilder = AutoDarkModeConfigBuilder.Instance();

        Updater updater = new Updater();
        bool update = false;
        readonly string curLanguage = Properties.Settings.Default.Language;

        public AboutWindow()
        {
            InitializeComponent();
            UiHandler();
        }

        private void UiHandler()
        {
            LangComBox.SelectedValue = Properties.Settings.Default.Language.ToString();
            SwitchDelayTB.Text = configBuilder.Config.Tunable.AccentColorSwitchDelay.ToString();
            if (Properties.Settings.Default.BackgroundUpdate)
            {
                BckgrUpdateCB.IsChecked = true;
            }
            if (Properties.Settings.Default.connectedStandby)
            {
                //conStandByCB.IsChecked = true;
            }
            if (SourceChord.FluentWPF.SystemTheme.AppTheme.Equals(SourceChord.FluentWPF.ApplicationTheme.Dark)){
                gitHubImage.Source = new BitmapImage(new Uri(@"Resources/GitHub_Logo_White.png", UriKind.RelativeOrAbsolute));
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!update)
            {
                updateInfoText.Text = Properties.Resources.msgSearchUpd;//searching for update...
                updateButton.IsEnabled = false;
                //todo: switch to command pipe, use updater.ParseResponse();
                if (updater.SilentUpdater())
                {
                    updateInfoText.Text = Properties.Resources.msgUpdateAvail;//a new update is available!
                    updateButton.Content = Properties.Resources.msgDownloadUpd;//Download update
                    update = true;
                    updateButton.IsEnabled = true;
                }
                else
                {
                    updateInfoText.Text = Properties.Resources.msgNoUpd;//no new updates are available.
                }
            }
            else
            {
                StartProcessByProcessInfo(updater.GetURL());
            }
        }

        private void TaskSchedulerLicense_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "MIT Copyright (c) 2003-2010 David Hall \n\n" +
                "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the 'Software'), " +
                "to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, " +
                "and/ or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: \n\n" +
                "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\n" +
                "THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, " +
                "WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
            MsgBox msgBox = new MsgBox(messageBoxText, "TaskSheduler License Information", "info", "close")
            {
                Owner = GetWindow(this)
            };
            msgBox.Show();
        }

        private void FluentWPF_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "MIT License Copyright(c) 2016 minami_SC\n\n" +
                "Permission is hereby granted, free of charge, to any person obtaining a copy" +
                "of this software and associated documentation files(the 'Software'), to deal in the Software without restriction, including without limitation the rights " +
                "to use, copy, modify, merge, publish, distribute, sublicense, and/ or sell" +
                "copies of the Software, and to permit persons to whom the Software is" +
                "furnished to do so, subject to the following conditions:\n\n" +
                "The above copyright notice and this permission notice shall be included in all" +
                "copies or substantial portions of the Software.\n\n" +
                "THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR" +
                "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY," +
                "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE" +
                "AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER" +
                "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM," +
                "OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
            MsgBox msgBox = new MsgBox(messageBoxText, "FluentWPF License Information", "info", "close")
            {
                Owner = GetWindow(this)
            };
            msgBox.Show();
        }

        private void TextBox_BlockChars_TextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void GitHubTextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            GitHubTextBlock.Foreground = Brushes.Blue;
            GitHubTextBlock.Cursor = Mouse.OverrideCursor = Cursors.Hand;
        }

        private void GitHubTextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            GitHubTextBlock.Foreground = Foreground;
            GitHubTextBlock.Cursor = Mouse.OverrideCursor = null;
        }

        private void GitHubTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo(@"https://github.com/Armin2208/Windows-Auto-Night-Mode");
        }

        private void TwitterTextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            TwitterTextBlock.Foreground = Brushes.Blue;
            TwitterTextBlock.Cursor = Mouse.OverrideCursor = Cursors.Hand;
        }

        private void TwitterTextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            TwitterTextBlock.Foreground = Foreground;
            TwitterTextBlock.Cursor = Mouse.OverrideCursor = null;
        }

        private void TwitterTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo(@"https://twitter.com/Armin2208");
        }

        private void PayPalTextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            PayPalTextBlock.Foreground = Brushes.Blue;
            PayPalTextBlock.Cursor = Mouse.OverrideCursor = Cursors.Hand;
        }

        private void PayPalTextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            PayPalTextBlock.Foreground = Foreground;
            PayPalTextBlock.Cursor = Mouse.OverrideCursor = null;
        }

        private void PayPalTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo(@"https://paypal.me/arminosaj");
        }

        private void TelegramTextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            telegramTextBlock.Foreground = Brushes.Blue;
            telegramTextBlock.Cursor = Mouse.OverrideCursor = Cursors.Hand;
        }

        private void TelegramTextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            telegramTextBlock.Foreground = Foreground;
            telegramTextBlock.Cursor = Mouse.OverrideCursor = null;
        }

        private void TelegramTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo(@"https://t.me/autodarkmode");
        }

        private void GitHubTextBlock_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) GitHubTextBlock_MouseLeftButtonDown(this, null);

        }

        private void PayPalTextBlock_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) PayPalTextBlock_MouseDown(this, null);
        }

        private void TelegramTextBlock_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TelegramTextBlock_MouseDown(this, null);
        }

        private void TwitterTextBlock_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TwitterTextBlock_MouseLeftButtonDown(this, null);
        }

        private void ComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            SetLanguage(LangComBox.SelectedValue.ToString());
            Translator.Text = Properties.Resources.lblTranslator;
            if (Properties.Settings.Default.Language != curLanguage)
            {
                RestartText.Text = Properties.Resources.restartNeeded;
            }
            else
            {
                RestartText.Text = null;
            }
        }

        private void SetLanguage(string lang)
        {
            Properties.Settings.Default.Language = lang;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(Properties.Settings.Default.Language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Language);
        }

        private void StartProcessByProcessInfo(string message)
        {
            Process.Start(new ProcessStartInfo(message)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void AboutWindowXAML_Closed(object sender, EventArgs e)
        {
            int accentColorSwitchDelay;
            try
            {
                accentColorSwitchDelay = int.Parse(SwitchDelayTB.Text);
                if (accentColorSwitchDelay < 100)
                {
                    accentColorSwitchDelay = 100;
                }
            } 
            catch (Exception ex)
            {
                accentColorSwitchDelay = -1;
                MsgBox.ShowErrorMessage(this, ex);
            }
            if (configBuilder.Config.Tunable.AccentColorSwitchDelay != accentColorSwitchDelay && accentColorSwitchDelay != -1)
            {
                configBuilder.Config.Tunable.AccentColorSwitchDelay = accentColorSwitchDelay;
                configBuilder.Save();
            }
            if (Properties.Settings.Default.Language != curLanguage)
            {                
                StartProcessByProcessInfo(Extensions.ExecutionPath);
                Application.Current.Shutdown();
            }
            else
            {
                Close();
            }
        }

        private void SwitchDelayTB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}
