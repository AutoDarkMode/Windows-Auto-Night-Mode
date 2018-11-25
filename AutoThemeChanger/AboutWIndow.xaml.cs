using System.Windows;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        Updater updater = new Updater();
        bool update = false;

        public AboutWindow()
        {
            InitializeComponent();
        }

        private void TwitterButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://twitter.com/Armin2208");
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode");
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!update)
            {
                updateInfoText.Text = "searching for update...";
                updateButton.IsEnabled = false;
                if (updater.SilentUpdater())
                {
                    updateInfoText.Text = "a new update is available!";
                    updateButton.Content = "Download Update";
                    update = true;
                    updateButton.IsEnabled = true;
                }
                else
                {
                    updateInfoText.Text = "no new updates are available.";
                }
            }
            else
            {
                System.Diagnostics.Process.Start(updater.GetURL());
            }
        }

        private void TaskShedulerLicense_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "MIT Copyright (c) 2003-2010 David Hall \n" +
                "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the 'Software'), " +
                "to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, " +
                "and/ or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: \n" +
                "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n" +
                "THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, " +
                "WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
            MessageBox.Show(messageBoxText, "TaskSheduler License Information");
        }
    }
}
