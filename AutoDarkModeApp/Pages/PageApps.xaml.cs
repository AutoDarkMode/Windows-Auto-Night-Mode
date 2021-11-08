using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using AutoDarkModeApp.Handlers;
using AutoDarkModeSvc.Communication;
using AutoDarkModeConfig;
using AdmProperties = AutoDarkModeConfig.Properties;

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
            try
            {
                builder.Load();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }

            //if a windows theme file was picked
            if (builder.Config.WindowsThemeMode.Enabled)
            {
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = AdmProperties.Resources.ToolTipDisabledDueTheme;
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

            //if the OS version is older than 1903
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
            //os version 1903+
            {
                //inform user about settings
                if (!builder.Config.WindowsThemeMode.Enabled) AccentColorCheckBox.ToolTip = AdmProperties.Resources.cbAccentColor;

                //numbox
                NumberBoxColorDelay.Value = Convert.ToInt32(builder.Config.SystemSwitch.Component.TaskbarSwitchDelay);

                AccentColorCheckBox.IsChecked = builder.Config.SystemSwitch.Component.TaskbarColorOnAdaptive;
                SystemComboBox_SelectionChanged(null, null);
            }

            //combobox
            if (builder.Config.AppsSwitch.Enabled)
            {
                AppComboBox.SelectedIndex = (int)builder.Config.AppsSwitch.Component.Mode;
            }
            else
            {
                AppComboBox.SelectedIndex = 3;
            }

            if (builder.Config.OfficeSwitch.Enabled)
            {
                OfficeComboBox.SelectedIndex = (int)builder.Config.OfficeSwitch.Component.Mode;
            }
            else
            {
                OfficeComboBox.SelectedIndex = 3;
            }


            //checkbox
            if (builder.Config.OfficeSwitch.Component.LightTheme == 5)
            {
                CheckBoxOfficeWhiteTheme.IsChecked = true;
            }
        }

        private void ShowErrorMessage(Exception ex)
        {
            string error = AdmProperties.Resources.errorThemeApply + "\n\nError ocurred in: " + ex.Source + "\n\n" + ex.Message;
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
                string result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.Switch, 15);
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
                builder.Config.SystemSwitch.Enabled = true;
                if (SystemComboBox.SelectedItem.Equals(SystemComboBoxItemSwitch))
                {
                    if (sender != null) builder.Config.SystemSwitch.Component.Mode = Mode.Switch;
                    AccentColorCheckBox.IsEnabled = true;
                    AccentColorCheckBox.Visibility = Visibility.Visible;
                    TextBlockColorDelay.Visibility = Visibility.Visible;
                    NumberBoxColorDelay.Visibility = Visibility.Visible;
                    StackPanelAdaptiveTaskbarAccent.Visibility = Visibility.Collapsed;
                }
                else if (SystemComboBox.SelectedItem.Equals(SystemComboBoxItemLightOnly))
                {
                    if (sender != null) builder.Config.SystemSwitch.Component.Mode = Mode.LightOnly;
                    AccentColorCheckBox.IsEnabled = false;
                    AccentColorCheckBox.Visibility = Visibility.Visible;
                    TextBlockColorDelay.Visibility = Visibility.Collapsed;
                    NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                    StackPanelAdaptiveTaskbarAccent.Visibility = Visibility.Collapsed;
                }
                else if (SystemComboBox.SelectedItem.Equals(SystemComboBoxItemDarkOnly))
                {
                    if (sender != null) builder.Config.SystemSwitch.Component.Mode = Mode.DarkOnly;
                    AccentColorCheckBox.IsEnabled = true;
                    AccentColorCheckBox.Visibility = Visibility.Visible;
                    TextBlockColorDelay.Visibility = Visibility.Collapsed;
                    NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                    StackPanelAdaptiveTaskbarAccent.Visibility = Visibility.Collapsed;
                }
                else if (SystemComboBox.SelectedItem.Equals(SystemComboBoxItemAccentOnly))
                {
                    if (sender != null) builder.Config.SystemSwitch.Component.Mode = Mode.AccentOnly;
                    AccentColorCheckBox.Visibility = Visibility.Collapsed;
                    if (!builder.Config.WindowsThemeMode.Enabled)
                    {
                        TextBlockColorDelay.Visibility = Visibility.Visible;
                        NumberBoxColorDelay.Visibility = Visibility.Visible;
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
                TextBlockColorDelay.Visibility = Visibility.Visible;
                NumberBoxColorDelay.Visibility = Visibility.Visible;
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
    }
}
