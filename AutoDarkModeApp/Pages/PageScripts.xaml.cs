#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeApp.Handlers;
using AutoDarkModeLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageScripts.xaml
    /// </summary>
    public partial class PageScripts : Page
    {
        private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private bool init = false;
        public PageScripts()
        {
            InitializeComponent();
            try
            {
                builder.LoadScriptConfig();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageScripts");
            }
            if (builder.ScriptConfig.Enabled)
            {
                ToggleSwitchScripts.IsOn = true;
            }
            StateUpdateHandler.OnScriptConfigUpdate += HandleConfigUpdate;
            StateUpdateHandler.StartScriptWatcher();

            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
            {
                OpenConfigCardIcon.FontFamily = new("Segoe Fluent Icons");
                DocCardIcon.FontFamily = new("Segoe Fluent Icons");
                RepoCardIcon.FontFamily = new("Segoe Fluent Icons");
            }

            init = true;
        }

        private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                StateUpdateHandler.StopScriptWatcher();
                init = false;
                try
                {
                    builder.LoadScriptConfig();
                    if (builder.ScriptConfig.Enabled)
                    {
                        ToggleSwitchScripts.IsOn = true;
                    }
                    else
                    {
                        ToggleSwitchScripts.IsOn = false;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageScripts HandleConfigUpdate");
                }
                init = true;
                StateUpdateHandler.StartScriptWatcher();
            });
        }

        private void ToggleSwitchScripts_Toggled(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!init) return;
            if (ToggleSwitchScripts.IsOn) builder.ScriptConfig.Enabled = true;
            else builder.ScriptConfig.Enabled = false;
            try
            {
                builder.SaveScripts();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageScripts");
            }
        }

        private void CardOpenScriptsFile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode", "scripts.yaml");
            new Process
            {
                StartInfo = new ProcessStartInfo(filepath)
                {
                    UseShellExecute = true
                }
            }.Start();
        }
        private void CardDocumentationLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ProcessHandler.StartProcessByProcessInfo("https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/wiki/How-to-add-custom-scripts");

        }

        private void CardScripts_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ProcessHandler.StartProcessByProcessInfo("https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/discussions/categories/custom-scripts");
        }
    }
}
