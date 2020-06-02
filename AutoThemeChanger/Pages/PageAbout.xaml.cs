using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace AutoThemeChanger.Pages
{
    /// <summary>
    /// Interaction logic for PageAbout.xaml
    /// </summary>
    public partial class PageAbout : Page
    {
        bool update = false;

        public PageAbout()
        {
            InitializeComponent();
            UiHandler();
        }

        private void UiHandler()
        {
            if (SourceChord.FluentWPF.SystemTheme.AppTheme.Equals(SourceChord.FluentWPF.ApplicationTheme.Dark))
            {
                gitHubImage.Source = new BitmapImage(new Uri(@"/Resources/GitHub_Logo_White.png", UriKind.Relative));
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            Updater updater = new Updater();

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
            MsgBox msgBox = new MsgBox(messageBoxText, "TaskSheduler License Information", "info", "close");
            msgBox.ShowDialog();
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
            MsgBox msgBox = new MsgBox(messageBoxText, "FluentWPF License Information", "info", "close");
            msgBox.ShowDialog();
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
    }
}
