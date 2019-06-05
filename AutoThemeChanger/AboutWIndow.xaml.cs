using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Globalization;
using System.Threading;
using System.Windows.Media.Imaging;
using System;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AboutWindow
    {
        Updater updater = new Updater();
        bool update = false;

        public AboutWindow()
        {
            InitializeComponent();
            darkTheme();

            switch (Properties.Settings.Default.Language.ToString())
            {
                case "de":
                    LangComBox.SelectedIndex = 0;
                    break;
                case "en":
                    LangComBox.SelectedIndex = 1;
                    break;
                case "it":
                    LangComBox.SelectedIndex = 2;
                    break;
                case "pl":
                    LangComBox.SelectedIndex = 3;
                    break;
            }
        }

        private void darkTheme()
        {
            RegEditHandler regEditHandler = new RegEditHandler();
            if (!regEditHandler.AppsUseLightTheme())
            {
                gitHubImage.Source = new BitmapImage(new Uri(@"Resources/GitHub_Logo_White.png", UriKind.RelativeOrAbsolute));
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!update)
            {
                updateInfoText.Text = Properties.Resources.msgSearchUpd;//searching for update...
                updateButton.IsEnabled = false;
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
                System.Diagnostics.Process.Start(updater.GetURL());
            }
        }

        private void TaskShedulerLicense_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "MIT Copyright (c) 2003-2010 David Hall \n\n" +
                "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the 'Software'), " +
                "to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, " +
                "and/ or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: \n\n" +
                "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\n" +
                "THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, " +
                "WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
            MessageBox.Show(messageBoxText, "TaskSheduler License Information");
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
            System.Diagnostics.Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode");
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
            System.Diagnostics.Process.Start("https://twitter.com/Armin2208");
            
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
            System.Diagnostics.Process.Start("https://paypal.me/arminosaj");
        }

        private void ComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            if (LangComBox.SelectedIndex == 0)
            {
                SetLanguage("de");
            }
            if (LangComBox.SelectedIndex == 1)
            {
                SetLanguage("en");
            }
            if(LangComBox.SelectedIndex == 2)
            {
                SetLanguage("it");
            }
            if(LangComBox.SelectedIndex == 3)
            {
                SetLanguage("pl");
            }
            RestartText.Text = Properties.Resources.restartNeeded;
            Translator.Text = Properties.Resources.lblTranslator;
        }
        private void SetLanguage(string lang)
        {
            Properties.Settings.Default.Language = lang;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(Properties.Settings.Default.Language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Language);
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
            MessageBox.Show(messageBoxText, "FluentWPF License Information");
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
            System.Diagnostics.Process.Start("https://t.me/autodarkmode");
        }
    }
}
