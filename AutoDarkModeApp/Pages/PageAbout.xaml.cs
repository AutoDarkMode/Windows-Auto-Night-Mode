using AutoDarkModeComms;
using AutoDarkModeSvc.Communication;
using SourceChord.FluentWPF;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AdmExtensions = AutoDarkModeConfig.Extensions;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageAbout.xaml
    /// </summary>
    public partial class PageAbout : Page
    {
        readonly Updater updater = new Updater(false);
        bool update = false;
        int easterEgg = 0;

        public PageAbout()
        {
            InitializeComponent();
            TextBoxVersionNumber.Text = "Beta 5";
            string commitHash = AdmExtensions.CommitHash();
            if (commitHash != "")
            {
                TextBoxVersionNumber.Text += $" - Backend-{commitHash}";
            }
            else
            {
                TextBoxVersionNumber.Text += " - Backend";
            }
            SystemTheme.ThemeChanged += SystemTheme_ThemeChanged;
            SystemTheme_ThemeChanged(this, null);
        }

        private void SystemTheme_ThemeChanged(object sender, EventArgs e)
        {
            if (SystemTheme.AppTheme.Equals(ApplicationTheme.Dark))
            {
                gitHubImage.Source = new BitmapImage(new Uri(@"/Resources/GitHub_Logo_White.png", UriKind.Relative));
            }
            else
            {
                gitHubImage.Source = new BitmapImage(new Uri(@"/Resources/GitHub_Logo_Black.png", UriKind.Relative));
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!update)
            {
                updateInfoText.Text = Properties.Resources.msgSearchUpd;//searching for update...
                updateButton.IsEnabled = false;
                updater.CheckNewVersion();
                if (updater.UpdateAvailable())
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
                updater.MessageBoxHandler();
            }
        }

        private void GitHubTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode");
        }

        private void TwitterTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo("https://twitter.com/Armin2208");

        }

        private void PayPalTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo("https://paypal.me/arminosaj");
        }

        private void TelegramTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo("https://t.me/autodarkmode");
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

        private void FluentWPFLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string messageBoxText = "MIT License \n\n Copyright(c) 2016 minami_SC\n\n" +
                "Permission is hereby granted, free of charge, to any person obtaining a copy" +
                "of this software and associated documentation files (the 'Software'), to deal in the Software without restriction, including without limitation the rights " +
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
            MsgBox msg = new MsgBox(messageBoxText, "FluentWPF License Information", "info", "close")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
        }

        private void FluentWPFLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) FluentWPFLicense_MouseDown(this, null);
        }

        private void TaskSchedulerLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string messageBoxText = "MIT Copyright (c) 2003-2010 David Hall \n\n" +
               "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), " +
               "to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, " +
               "and/ or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: \n\n" +
               "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\n" +
               "THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
               "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, " +
               "WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
            MsgBox msg = new MsgBox(messageBoxText, "TaskSheduler License Information", "info", "close")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
        }

        private void TaskSchedulerLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TaskSchedulerLicense_MouseDown(this, null);
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            easterEgg++;
            if (easterEgg == 1) TextBoxVersionNumber.Foreground = Brushes.Orange;
            if (easterEgg == 2) TextBoxVersionNumber.Foreground = Brushes.Red;
            if (easterEgg == 3) TextBoxVersionNumber.Foreground = Brushes.Green;
            if (easterEgg == 4) TextBoxVersionNumber.Foreground = Brushes.Blue;
            if (easterEgg == 5)
            {
                StartProcessByProcessInfo("https://bit.ly/qgraphics");
                if (easterEgg == 1) TextBoxVersionNumber.Foreground = Brushes.Black;
                easterEgg = 0;
            }
        }

        private void InputSimulatorLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string MessageBoxText = "MIT License \n\n Copyright(c) 2019 Michael Noonan \n\n" +
                "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), " +
                "to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/ or " +
                "sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: \n\n" +
                "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\n" +
                "THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER " +
                "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
            MsgBox msg = new MsgBox(MessageBoxText, "InputSimulator License Information", "info", "close")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
        }

        private void InputSimulatorLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) InputSimulatorLicense_MouseDown(this, null);
        }

        private void StartProcessByProcessInfo(string message)
        {
            Process.Start(new ProcessStartInfo(message)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void YamlDotNetLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string MessageBoxText = "The MIT License (MIT) \n\n" +
                "Copyright (c) 2008, 2009, 2010, 2011, 2012, 2013, 2014 Antoine Aubry and contributors \n\n" +
                "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), " +
                "to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/ or " +
                "sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: \n\n" +
                "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\n" +
                "THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER " +
                "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
            MsgBox msg = new MsgBox(MessageBoxText, "YamlDotNet License Information", "info", "close")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
        }

        private void YamlDotNetLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) YamlDotNetLicense_MouseDown(this, null);
        }

        private void NetMQLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string MessageBoxText = "Copyright (C) 2007 Free Software Foundation, Inc.";
            MsgBox msg = new MsgBox(MessageBoxText, "", "info", "close");
            msg.Owner = Window.GetWindow(this);
            msg.ShowDialog();
        }

        private void NetMQLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) NetMQLicense_MouseDown(this, null);
        }

        private void SunriseCalcLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string MessageBoxText = "The MIT License (MIT) \n\n" +
                "Copyright (c) 2021 John Reid \n\n" +
                "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), " +
                "to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/ or " +
                "sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: \n\n" +
                "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\n" +
                "THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER " +
                "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
            MsgBox msg = new MsgBox(MessageBoxText, "SunriseCalc License Information", "info", "close");
            msg.Owner = Window.GetWindow(this);
            msg.ShowDialog();
        }

        private void SunriseCalcLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SunriseCalcLicense_MouseDown(this, null);
        }

        private void ModernWPFLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string MessageBoxText = "The MIT License (MIT) \n\n" +
                "Copyright (c) 2019 Yimeng Wu \n\n" +
                "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), " +
                "to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/ or " +
                "sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: \n\n" +
                "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\n" +
                "THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER " +
                "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
            MsgBox msg = new MsgBox(MessageBoxText, "ModernWPF License Information", "info", "close");
            msg.Owner = Window.GetWindow(this);
            msg.ShowDialog();
        }

        private void ModernWPFLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ModernWPFLicense_MouseDown(this, null);
        }

        private void NLogLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string MessageBoxText = "Copyright (c) 2004-2021 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen \n\n" +
                "All rights reserved. \n\n" +
                "Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met: \n\n" +
                "* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. \n\n" +
                "* Redistributions in binary form must reproduce the above copyright notice," +
                "this list of conditions and the following disclaimer in the documentation" +
                "and / or other materials provided with the distribution. \n\n" +
                "* Neither the name of Jaroslaw Kowalski nor the names of its contributors may be used to endorse or promote products derived from this " +
                "software without specific prior written permission. \n\n" +
                "THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'" +
                "AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE" +
                "IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE" +
                "ARE DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE" +
                "LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR" +
                "CONSEQUENTIAL DAMAGES(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF" +
                "SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS" +
                "INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN" +
                "CONTRACT, STRICT LIABILITY, OR TORT(INCLUDING NEGLIGENCE OR OTHERWISE)" +
                "ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.";
            MsgBox msg = new MsgBox(MessageBoxText, "NLog License Information", "info", "close");
            msg.Owner = Window.GetWindow(this);
            msg.ShowDialog();
        }

        private void NLogLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) NLogLicense_MouseDown(this, null);
        }
    }
}
