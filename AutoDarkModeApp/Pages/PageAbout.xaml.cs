using AutoDarkModeComms;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using SourceChord.FluentWPF;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
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
        private int easterEgg;
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        private VersionInfo versionInfo = new();

        public PageAbout()
        {
            InitializeComponent();

            UpdateVersionNumbers();

            SystemTheme.ThemeChanged += SystemTheme_ThemeChanged;
            SystemTheme_ThemeChanged(this, null);
        }

        private void UpdateVersionNumbers()
        {
            TextBlockCommitHash.Text = "Commit: " + versionInfo.Commit;
            TextBlockAppVersion.Text = "App: " + versionInfo.App;
            TextBlockSvcVersion.Text = "Service: " + versionInfo.Svc;
            TextBlockUpdaterVersion.Text = "Updater: " + versionInfo.Updater;
            TextBlockShellVersion.Text = "Shell: " + versionInfo.Shell;
            TextBlockNetCoreVersion.Text = ".Net: " + versionInfo.NetCore;
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

        /*
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
                    if (AdmExtensions.InstallModeUsers() && updater.CanUseUpdater())
                    {
                        updateButton.Content = "Update Available";
                        updateButton.IsEnabled = false;
                    }
                    else
                    {
                        updateButton.Content = Properties.Resources.msgDownloadUpd;//Download update
                        updateButton.IsEnabled = true;
                    }
                    update = true;
                }
                else
                {
                    updateInfoText.Text = Properties.Resources.msgNoUpd;//no new updates are available.
                }
            }
            else
            {
                updater.Update();
            }
        }
        */

        private void GitHubTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode");
        }

        private void TelegramTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo("https://t.me/autodarkmode");
        }

        private void GitHubTextBlock_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) GitHubTextBlock_MouseLeftButtonDown(this, null);

        }

        private void TelegramTextBlock_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TelegramTextBlock_MouseDown(this, null);
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
            MsgBox msg = new(messageBoxText, "FluentWPF License Information", "info", "close")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
        }

        private void SharpromptLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string messageBoxText = "MIT License \n\n Copyright(c) 2019 shibayanC\n\n" +
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
            MsgBox msg = new(messageBoxText, "Sharprompt License Information", "info", "close")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
        }

        private void SharpromptLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SharpromptLicense_MouseDown(this, null);
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
            MsgBox msg = new(messageBoxText, "TaskSheduler License Information", "info", "close")
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
            MsgBox msg = new(MessageBoxText, "InputSimulator License Information", "info", "close")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
        }

        private void InputSimulatorLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) InputSimulatorLicense_MouseDown(this, null);
        }

        private static void StartProcessByProcessInfo(string message)
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
            MsgBox msg = new(MessageBoxText, "YamlDotNet License Information", "info", "close")
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
            string text = "Copyright (C) 2007 Free Software Foundation, Inc \n\n";
            text += File.ReadAllText(Path.Combine(AdmExtensions.ExecutionDir, "Licenses" , "lgpl.txt"));
            MsgBox msg = new(text, "", "info", "close")
            {
                Owner = Window.GetWindow(this)
            };
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
            MsgBox msg = new(MessageBoxText, "SunriseCalc License Information", "info", "close");
            msg.Owner = Window.GetWindow(this);
            _ = msg.ShowDialog();
        }

        private void SunriseSunsetLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SunriseSunsetLicense_MouseDown(this, null);
        }

        private void SunriseSunsetLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string text = "Copyright(c) 2017 Mursaat \n\n";
            text += File.ReadAllText(Path.Combine(AdmExtensions.ExecutionDir, "Licenses", "apache-2.0.txt"));
            MsgBox msg = new(text, "SunriseSunset License Information", "info", "close");
            msg.Owner = Window.GetWindow(this);
            _ = msg.ShowDialog();
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
            MsgBox msg = new(MessageBoxText, "ModernWPF License Information", "info", "close");
            msg.Owner = Window.GetWindow(this);
            _ = msg.ShowDialog();
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
            MsgBox msg = new(MessageBoxText, "NLog License Information", "info", "close")
            {
                Owner = Window.GetWindow(this)
            };
            _ = msg.ShowDialog();
        }

        private void NLogLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) NLogLicense_MouseDown(this, null);
        }

        private void UpdaterLicense_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) UpdaterLicense_MouseDown(sender, null);
        }

        private void UpdaterLicense_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string MessageBoxText = "";
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                using Process p = new();
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.Arguments = "--info";
                startInfo.FileName = AdmExtensions.ExecutionPathUpdater;
                p.StartInfo = startInfo;
                _ = p.Start();
                MessageBoxText += p.StandardOutput.ReadToEnd();
            }
            catch (Exception)
            {
                MessageBoxText += "Updater missing!\n";
            }
           
            MessageBoxText += "\nThe MIT License (MIT) \n\n" +
                "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), " +
                "to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/ or " +
                "sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: \n\n" +
                "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. \n\n" +
                "THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER " +
                "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";


            MsgBox msg = new(MessageBoxText, "Updater License Information", "info", "close")
            {
                Owner = Window.GetWindow(this)
            };
            _ = msg.ShowDialog();
        }
        private void ButtonCopyVersionInfo_Click(object sender, RoutedEventArgs e)
        {
            // most likely use case is to paste in an issue, so
            // we create a markddown string that will look nice
            // in that context
            var versionText = new StringBuilder()
                .Append("- Commit: `")
                .Append(versionInfo.Commit)
                .AppendLine("`")
                .Append("- App: `")
                .Append(versionInfo.App)
                .AppendLine("`")
                .Append("- Service: `")
                .Append(versionInfo.Svc)
                .AppendLine("`")
                .Append("- Updater: `")
                .Append(versionInfo.Updater)
                .AppendLine("`")
                .Append("- Shell: `")
                .Append(versionInfo.Shell)
                .AppendLine("`")
                .Append("- .Net: `")
                .Append(versionInfo.NetCore)
                .AppendLine("`");

            Clipboard.SetData(DataFormats.Text, versionText);
        }

        private class VersionInfo
        {
            public string Commit { get; }
            public string App { get; }
            public string Svc { get; }
            public string Updater { get; }
            public string Shell { get; }
            public string NetCore { get; }

            public VersionInfo()
            {
                var currentDirectory = AdmExtensions.ExecutionDir;

                Commit = AdmExtensions.CommitHash();

                App = ValueOrNotFound(() => Assembly.GetExecutingAssembly().GetName().Version.ToString());

                Svc = ValueOrNotFound(() => FileVersionInfo.GetVersionInfo(currentDirectory + @"\AutoDarkModeSvc.exe").FileVersion);
                Updater = ValueOrNotFound(() => FileVersionInfo.GetVersionInfo(currentDirectory + @"\Updater\AutoDarkModeUpdater.exe").FileVersion);
                Shell = ValueOrNotFound(() => FileVersionInfo.GetVersionInfo(currentDirectory + @"\AutoDarkModeShell.exe").FileVersion);
                NetCore = ValueOrNotFound(() => Environment.Version.ToString());

                static string ValueOrNotFound(Func<string> value)
                {
                    try
                    {
                        return value();
                    }
                    catch
                    {
                        return "not found";
                    }
                }

            }
        }

    }
}
