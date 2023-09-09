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
using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using AutoDarkModeApp.Handlers;
using AutoDarkModeSvc.Communication;
using AutoDarkModeLib;
using AdmProperties = AutoDarkModeLib.Properties;
using System.Windows.Input;

namespace AutoDarkModeApp
{
    /// <summary>
    /// Interaction logic for PageApps.xaml
    /// </summary>
    public partial class PageApps : Page
    {
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private bool is1903 = false;
        private readonly bool init = true;

        public PageApps()
        {
            InitializeComponent();
            UiHandler();
            init = false;
        }

        private void UiHandler()
        {
            // load config
            try
            {
                builder.Load();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }

            // Use new Fluent Icons on machines with Windows 11 installed
            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
            {
                TextBlockApps.FontFamily = new("Segoe Fluent Icons");
                TextBlockSystem.FontFamily = new("Segoe Fluent Icons");
                TextBlockTouchKeyboard.FontFamily = new("Segoe Fluent Icons");
            }
            else
            {
                CardTouchKeyboard.Visibility = Visibility.Collapsed;
            }

            // If a windows managed theme file was picked, block some settings
            if (builder.Config.WindowsThemeMode.Enabled)
            {
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = AdmProperties.Resources.ToolTipDisabledDueTheme;
                TextBlockOfficeLabel.ToolTip = AdmProperties.Resources.ToolTipOfficeDisclaimer;
                SystemComboBoxItemSwitch.ToolTip = AdmProperties.Resources.ToolTipDisabledDueTheme;
                SystemComboBoxItemLightOnly.ToolTip = AdmProperties.Resources.ToolTipDisabledDueTheme;
                SystemComboBoxItemLightOnly.IsEnabled = false;
                SystemComboBoxItemSwitch.IsEnabled = false;
                SystemComboBoxItemDarkOnly.IsEnabled = false;
                AppComboBox.IsEnabled = false;
                AppComboBox.ToolTip = AdmProperties.Resources.ToolTipDisabledDueTheme;
                NumberBoxColorDelay.IsEnabled = false;
                NumberBoxColorDelay.ToolTip = AdmProperties.Resources.ToolTipDisabledDueTheme;
            }

            if (builder.Config.SystemSwitch.Enabled)
            {
                switch (builder.Config.SystemSwitch.Component.Mode)
                {
                    case Mode.Switch:
                        if (builder.Config.WindowsThemeMode.Enabled) SystemComboBox.SelectedItem = SystemComboBoxItemDisabled;
                        else SystemComboBox.SelectedItem = SystemComboBoxItemSwitch;
                        break;
                    case Mode.LightOnly:
                        if (builder.Config.WindowsThemeMode.Enabled) SystemComboBox.SelectedItem = SystemComboBoxItemDisabled;
                        else SystemComboBox.SelectedItem = SystemComboBoxItemLightOnly;
                        break;
                    case Mode.DarkOnly:
                        if (builder.Config.WindowsThemeMode.Enabled) SystemComboBox.SelectedItem = SystemComboBoxItemDisabled;
                        else SystemComboBox.SelectedItem = SystemComboBoxItemDarkOnly;
                        break;
                    case Mode.AccentOnly:
                        SystemComboBox.SelectedItem = SystemComboBoxItemAccentOnly;
                        break;
                }
                RadioButtonAdaptiveTaskbarAccentOnDark.IsChecked = builder.Config.SystemSwitch.Component.TaskbarColorWhenNonAdaptive == Theme.Dark;
                RadioButtonAdaptiveTaskbarAccentOnLight.IsChecked = builder.Config.SystemSwitch.Component.TaskbarColorWhenNonAdaptive == Theme.Light;
            }
            else
            {
                SystemComboBox.SelectedItem = SystemComboBoxItemDisabled;
            }

            if (builder.Config.SystemSwitch.Component.DWMPrevalenceSwitch)
            {
                CheckBoxDWMPrevalence.IsChecked = true;
            }
            else
            {
                StackPanelDWMPrevalenceOptions.IsEnabled = false;
            }
            RadioButtonDWMPrevalenceOnDark.IsChecked = builder.Config.SystemSwitch.Component.DWMPrevalenceEnableTheme == Theme.Dark;
            RadioButtonDWMPrevalenceOnLight.IsChecked = builder.Config.SystemSwitch.Component.DWMPrevalenceEnableTheme == Theme.Light;


            //if the OS version is older than 1903 block access to System Combobox
            if (int.Parse(RegistryHandler.GetOSversion()).CompareTo(1900) > 0) is1903 = true;
            if (!is1903)
            {
                SystemComboBox.IsEnabled = false;
                SystemComboBox.ToolTip = AdmProperties.Resources.cmb1903;
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = AdmProperties.Resources.cmb1903;
                builder.Config.SystemSwitch.Enabled = false;
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex);
                }
            }
            else
            {
                //inform user about settings
                if (!builder.Config.WindowsThemeMode.Enabled) AccentColorCheckBox.ToolTip = AdmProperties.Resources.cbAccentColor;

                //numbox
                NumberBoxColorDelay.Value = Convert.ToInt32(builder.Config.SystemSwitch.Component.TaskbarSwitchDelay);

                SystemComboBox_SelectionChanged(null, null);
            }

            // Initialize App combobox
            if (builder.Config.AppsSwitch.Enabled)
            {
                AppComboBox.SelectedIndex = (int)builder.Config.AppsSwitch.Component.Mode;
            }
            else
            {
                AppComboBox.SelectedIndex = 3;
            }

            // Initialize Office combobox
            if (builder.Config.OfficeSwitch.Enabled)
            {
                if (builder.Config.OfficeSwitch.Component.Mode == Mode.FollowSystemTheme)
                {
                    OfficeComboBox.SelectedIndex = 3;
                    CheckBoxOfficeWhiteTheme.IsEnabled = false;
                }
                else OfficeComboBox.SelectedIndex = (int)builder.Config.OfficeSwitch.Component.Mode;
            }
            else
            {
                OfficeComboBox.SelectedIndex = 4;
            }

            // Office checkbox
            if (builder.Config.OfficeSwitch.Component.LightTheme == 5)
            {
                CheckBoxOfficeWhiteTheme.IsChecked = true;
            }

            // Initialize Touch Keyboard Toggle
            if (builder.Config.TouchKeyboardSwitch.Enabled)
            {
                ToggleTouchkeyboard.IsOn = true;
            }
            else 
            { 
                ToggleTouchkeyboard.IsOn = false; 
            }

            // Initialize Color Filter Toggle
            if (builder.Config.ColorFilterSwitch.Enabled)
            {
                ToggleColorFilter.IsOn = true;
            }
            else
            {
                ToggleColorFilter.IsOn = false;
            }

        }

        private void ShowErrorMessage(Exception ex)
        {
            string error = AdmProperties.Resources.ErrorMessageBox + "\n\nError ocurred in: " + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new(error, AdmProperties.Resources.errorOcurredTitle, "error", "yesno")
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

        private async void RequestThemeSwitch()
        {
            try
            {
                string result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
                if (result != StatusCode.Ok)
                {
                    throw new SwitchThemeException(result, "PageApps");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        private void AppComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!init)
            {
                builder.Config.AppsSwitch.Enabled = true;
                if (AppComboBox.SelectedIndex.Equals(0))
                {
                    builder.Config.AppsSwitch.Component.Mode = Mode.Switch;
                    AccentColorCheckBox.IsEnabled = true;
                }
                else if (AppComboBox.SelectedIndex.Equals(1))
                {
                    builder.Config.AppsSwitch.Component.Mode = Mode.LightOnly;
                    AccentColorCheckBox.IsEnabled = true;
                }
                else if (AppComboBox.SelectedIndex.Equals(2))
                {
                    builder.Config.AppsSwitch.Component.Mode = Mode.DarkOnly;
                    AccentColorCheckBox.IsEnabled = true;
                }
                else if (AppComboBox.SelectedIndex.Equals(3))
                {
                    builder.Config.AppsSwitch.Enabled = false;
                    AccentColorCheckBox.IsEnabled = false;
                }

                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex);
                }
                RequestThemeSwitch();
            }
        }

        private void SystemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!init || sender == null)
            {
                if (SystemComboBox.SelectedItem.Equals(SystemComboBoxItemSwitch))
                {
                    builder.Config.SystemSwitch.Enabled = true;
                    if (sender != null) builder.Config.SystemSwitch.Component.Mode = Mode.Switch;
                    AccentColorCheckBox.IsEnabled = true;
                    AccentColorCheckBox.Visibility = Visibility.Visible;
                    AccentColorCheckBox.IsChecked = builder.Config.SystemSwitch.Component.TaskbarColorOnAdaptive;
                    if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.MinBuildForNewFeatures)
                    {
                        NumberBoxColorDelay.Visibility = NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                        TextBlockColorDelay.Visibility = Visibility.Collapsed;
                    }
                    else if (AccentColorCheckBox.IsChecked ?? false)
                    {
                        if (Environment.OSVersion.Version.Build < (int)WindowsBuilds.MinBuildForNewFeatures)
                        {
                            TextBlockColorDelay.Visibility = Visibility.Visible;
                            NumberBoxColorDelay.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        NumberBoxColorDelay.Visibility = NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                        TextBlockColorDelay.Visibility = Visibility.Collapsed;
                    }
                    StackPanelAdaptiveTaskbarAccent.Visibility = Visibility.Collapsed;
                }
                else if (SystemComboBox.SelectedItem.Equals(SystemComboBoxItemLightOnly))
                {
                    builder.Config.SystemSwitch.Enabled = true;
                    if (sender != null) builder.Config.SystemSwitch.Component.Mode = Mode.LightOnly;
                    AccentColorCheckBox.IsEnabled = false;
                    AccentColorCheckBox.Visibility = Visibility.Visible;
                    TextBlockColorDelay.Visibility = Visibility.Collapsed;
                    NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                    StackPanelAdaptiveTaskbarAccent.Visibility = Visibility.Collapsed;
                }
                else if (SystemComboBox.SelectedItem.Equals(SystemComboBoxItemDarkOnly))
                {
                    builder.Config.SystemSwitch.Enabled = true;
                    if (sender != null) builder.Config.SystemSwitch.Component.Mode = Mode.DarkOnly;
                    AccentColorCheckBox.IsEnabled = true;
                    AccentColorCheckBox.Visibility = Visibility.Visible;
                    TextBlockColorDelay.Visibility = Visibility.Collapsed;
                    NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                    StackPanelAdaptiveTaskbarAccent.Visibility = Visibility.Collapsed;
                }
                else if (SystemComboBox.SelectedItem.Equals(SystemComboBoxItemAccentOnly))
                {
                    builder.Config.SystemSwitch.Enabled = true;
                    if (sender != null) builder.Config.SystemSwitch.Component.Mode = Mode.AccentOnly;
                    AccentColorCheckBox.Visibility = Visibility.Collapsed;
                    if (!builder.Config.WindowsThemeMode.Enabled)
                    {
                        if (Environment.OSVersion.Version.Build < (int)WindowsBuilds.MinBuildForNewFeatures)
                        {
                            TextBlockColorDelay.Visibility = Visibility.Visible;
                            NumberBoxColorDelay.Visibility = Visibility.Visible;
                        }                        
                        else
                        {
                            TextBlockColorDelay.Visibility = Visibility.Collapsed;
                            NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        TextBlockColorDelay.Visibility = Visibility.Collapsed;
                        NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                    }
                    StackPanelAdaptiveTaskbarAccent.Visibility = Visibility.Visible;
                }
                else if (SystemComboBox.SelectedItem.Equals(SystemComboBoxItemDisabled))
                {
                    builder.Config.SystemSwitch.Enabled = false;
                    if (sender != null) builder.Config.SystemSwitch.Enabled = false;
                    AccentColorCheckBox.IsEnabled = false;
                    AccentColorCheckBox.Visibility = Visibility.Visible;
                    TextBlockColorDelay.Visibility = Visibility.Collapsed;
                    NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                    StackPanelAdaptiveTaskbarAccent.Visibility = Visibility.Collapsed;
                }
                if (sender != null)
                {
                    try
                    {
                        builder.Save();
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage(ex);
                    }
                    RequestThemeSwitch();
                }
            }
        }

        private void AccentColorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked ?? false)
            {
                builder.Config.SystemSwitch.Component.TaskbarColorOnAdaptive = true;
                if (Environment.OSVersion.Version.Build < (int)WindowsBuilds.MinBuildForNewFeatures)
                {
                    TextBlockColorDelay.Visibility = Visibility.Visible;
                    NumberBoxColorDelay.Visibility = Visibility.Visible;
                }
                else
                {
                    TextBlockColorDelay.Visibility = Visibility.Collapsed;
                    NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                builder.Config.SystemSwitch.Component.TaskbarColorOnAdaptive = false;
                TextBlockColorDelay.Visibility = Visibility.Collapsed;
                NumberBoxColorDelay.Visibility = Visibility.Collapsed;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void NumberBoxColorDelay_ValueChanged(ModernWpf.Controls.NumberBox sender, ModernWpf.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!init)
            {
                if (double.IsNaN(NumberBoxColorDelay.Value)) //fixes crash when leaving box empty and clicking outside it
                    return;

                builder.Config.SystemSwitch.Component.TaskbarSwitchDelay = Convert.ToInt32(NumberBoxColorDelay.Value);
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex);
                }
            }
        }

        private void OfficeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!init)
            {
                builder.Config.OfficeSwitch.Enabled = true;
                CheckBoxOfficeWhiteTheme.IsEnabled = true;
                if (OfficeComboBox.SelectedIndex.Equals(0))
                {
                    builder.Config.OfficeSwitch.Component.Mode = Mode.Switch;
                }
                else if (OfficeComboBox.SelectedIndex.Equals(1))
                {
                    builder.Config.OfficeSwitch.Component.Mode = Mode.LightOnly;
                }
                else if (OfficeComboBox.SelectedIndex.Equals(2))
                {
                    builder.Config.OfficeSwitch.Component.Mode = Mode.DarkOnly;
                }
                else if (OfficeComboBox.SelectedIndex.Equals(3))
                {
                    builder.Config.OfficeSwitch.Component.Mode = Mode.FollowSystemTheme;
                    CheckBoxOfficeWhiteTheme.IsEnabled = false;
                }
                else if (OfficeComboBox.SelectedIndex.Equals(4))
                {
                    builder.Config.OfficeSwitch.Enabled = false;
                }
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex);
                }
                RequestThemeSwitch();
            }

        }

        private void CheckBoxOfficeWhiteTheme_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxOfficeWhiteTheme.IsChecked ?? true)
            {
                builder.Config.OfficeSwitch.Component.LightTheme = 5;
            }
            else
            {
                builder.Config.OfficeSwitch.Component.LightTheme = 0;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void ButtonWikiBrowserExtension_Click(object sender, RoutedEventArgs e)
        {
            StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Dark-Mode-for-Webbrowser");
        }

        private static void StartProcessByProcessInfo(string message)
        {
            Process.Start(new ProcessStartInfo(message)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void RadioButtonAdaptiveTaskbarAccentOnLight_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.SystemSwitch.Component.TaskbarColorWhenNonAdaptive = Theme.Light;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void RadioButtonAdaptiveTaskbarAccentOnDark_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.SystemSwitch.Component.TaskbarColorWhenNonAdaptive = Theme.Dark;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void CheckBoxDWMPrevalence_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked ?? false)
            {
                builder.Config.SystemSwitch.Component.DWMPrevalenceSwitch = true;
                StackPanelDWMPrevalenceOptions.IsEnabled = true;
            }
            else
            {
                builder.Config.SystemSwitch.Component.DWMPrevalenceSwitch = false;
                StackPanelDWMPrevalenceOptions.IsEnabled = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void RadioButtonDWMPrevalenceOnLight_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.SystemSwitch.Component.DWMPrevalenceEnableTheme = Theme.Light;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void RadioButtonDWMPrevalenceOnDark_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.SystemSwitch.Component.DWMPrevalenceEnableTheme = Theme.Dark;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void ToggleTouchkeyboard_Toggled(object sender, RoutedEventArgs e)
        {
            if (init) return;

            builder.Config.TouchKeyboardSwitch.Enabled = ToggleTouchkeyboard.IsOn;
            SaveConfig();
        }

        private void SaveConfig()
        {
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void ToggleColorFilter_Toggled(object sender, RoutedEventArgs e)
        {
            if (init) return;
            builder.Config.ColorFilterSwitch.Enabled = ToggleColorFilter.IsOn;
            SaveConfig();
        }

        private void ButtonRecommendedApps_Click(object sender, RoutedEventArgs e)
        {
            StartProcessByProcessInfo("https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/wiki/Apps-with-Auto-Dark-Mode-support");
        }
    }
}
