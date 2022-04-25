using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoDarkModeConfig;
using Windows.System.Power;
using System.Diagnostics;
using AdmProperties = AutoDarkModeConfig.Properties;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageSwitchModes.xaml
    /// </summary>
    public partial class PageSwitchModes : Page
    {
        private readonly bool init = true;
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        public PageSwitchModes()
        {
            InitializeComponent();

            //ui init

            //deactivate some incompatible settings
            if (PowerManager.BatteryStatus == BatteryStatus.NotPresent)
            {
                CheckBoxBatteryDarkMode.IsEnabled = false;
            }

            if (builder.Config.GPUMonitoring.Enabled)
            {
                CheckBoxGPUMonitoring.IsChecked = true;
                StackPanelGPUMonitoring.Visibility = Visibility.Visible;
            }
            else
            {
                CheckBoxGPUMonitoring.IsChecked = false;
                StackPanelGPUMonitoring.Visibility = Visibility.Collapsed;
            }
            ComboBoxGPUSamples.SelectedIndex = builder.Config.GPUMonitoring.Samples - 1;
            NumberBoxGPUThreshold.Value = Convert.ToDouble(builder.Config.GPUMonitoring.Threshold);

            CheckBoxBatteryDarkMode.IsChecked = builder.Config.Events.DarkThemeOnBattery;

            HotkeyTextboxForceDark.Text = builder.Config.Hotkeys.ForceDarkHotkey ?? "";
            HotkeyTextboxForceLight.Text = builder.Config.Hotkeys.ForceLightHotkey ?? "";
            HotkeyTextboxNoForce.Text = builder.Config.Hotkeys.NoForceHotkey ?? "";
            ToggleHotkeys.IsOn = builder.Config.Hotkeys.Enabled;
            TextBlockHotkeyEditHint.Visibility = ToggleHotkeys.IsOn ? Visibility.Visible : Visibility.Hidden;

            init = false;
        }

        private void CheckBoxBatteryDarkMode_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxBatteryDarkMode.IsChecked.Value)
            {
                builder.Config.Events.DarkThemeOnBattery = true;
            }
            else
            {
                builder.Config.Events.DarkThemeOnBattery = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxBatteryDarkMode_Click");
            }
        }

        private void ShowErrorMessage(Exception ex, string location)
        {
            string error = AdmProperties.Resources.errorThemeApply + $"\n\nError ocurred in: {location}" + ex.Source + "\n\n" + ex.Message;
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

        private void CheckBoxGPUMonitoring_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxGPUMonitoring.IsChecked.Value)
            {
                builder.Config.GPUMonitoring.Enabled = true;
                StackPanelGPUMonitoring.Visibility = Visibility.Visible;
            }
            else
            {
                builder.Config.GPUMonitoring.Enabled = false;
                StackPanelGPUMonitoring.Visibility = Visibility.Collapsed;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxGPUMonitoring_Click");
            }
        }

        private void NumberBoxGPUThreshold_ValueChanged(ModernWpf.Controls.NumberBox sender, ModernWpf.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!init)
            {
                if (double.IsNaN(NumberBoxGPUThreshold.Value)) //fixes crash when leaving box empty and clicking outside it
                    return;

                builder.Config.GPUMonitoring.Threshold = Convert.ToInt32(NumberBoxGPUThreshold.Value);

                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex, "NumberBoxGPUThreshold_ValueChanged");
                }
            }
        }

        private void ComboBoxGPUSamples_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            builder.Config.GPUMonitoring.Samples = ComboBoxGPUSamples.SelectedIndex + 1;
            if (init) return;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "ComboBoxGPUSamples_DropDownClosed");
            }
        }

        private void ToggleHotkeys_Toggled(object sender, RoutedEventArgs e)
        {
            TextBlockHotkeyEditHint.Visibility = ToggleHotkeys.IsOn ? Visibility.Visible : Visibility.Hidden;
            if (ToggleHotkeys.IsOn) GridHotkeys.IsEnabled = false;
            else GridHotkeys.IsEnabled = true;
            if (init) return;
            builder.Config.Hotkeys.Enabled = ToggleHotkeys.IsOn;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "toggle_hotkeys");
            }
        }

        private void HotkeyTextboxNoForce_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string hotkeyString = GetHotkeyString(e);
            if (sender is TextBox tb)
            {
                tb.Text = hotkeyString;
            }
            if (hotkeyString == builder.Config.Hotkeys.NoForceHotkey) return;
            builder.Config.Hotkeys.NoForceHotkey = hotkeyString;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "hotkeybox_noforce");
            }
        }

        private void HotkeyTextboxForceDark_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string hotkeyString = GetHotkeyString(e);
            if (sender is TextBox tb)
            {
                tb.Text = hotkeyString;
            }
            if (hotkeyString == builder.Config.Hotkeys.ForceDarkHotkey) return;
            builder.Config.Hotkeys.ForceDarkHotkey = hotkeyString;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "hotkeybox_forcedark");
            }
        }

        private void HotkeyTextboxForceLight_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string hotkeyString = GetHotkeyString(e);
            if (sender is TextBox tb)
            {
                tb.Text = hotkeyString;
            }
            if (hotkeyString == builder.Config.Hotkeys.ForceLightHotkey) return;
            builder.Config.Hotkeys.ForceLightHotkey = hotkeyString;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "hotkeybox_forcelight");
            }
        }

        private static string GetHotkeyString(KeyEventArgs e)
        {
            e.Handled = true;
            Key key = e.Key;
            string keyString = e.Key.ToString();

            if (keyString.Contains("Alt") || keyString.Contains("Shift") || keyString.Contains("Win") || keyString.Contains("Ctrl") || keyString.Contains("System"))
            {
                return null;
            }

            ModifierKeys modifiers = Keyboard.Modifiers;
            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            {
                modifiers |= ModifierKeys.Windows;
            }
            string isShift = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? "Shift + " : "";
            string isCtrl = (modifiers & ModifierKeys.Control) == ModifierKeys.Control ? "Ctrl + " : "";
            string isWin = (modifiers & ModifierKeys.Windows) == ModifierKeys.Windows ? "LWin + " : "";
            string isAlt = (modifiers & ModifierKeys.Alt) == ModifierKeys.Alt ? "Alt + " : "";
            string modifiersString = $"{isCtrl}{isShift}{isAlt}{isWin}";
            return modifiersString.Length > 0 ? $"{modifiersString}{key}" : null;
        }
    }
}
