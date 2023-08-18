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
using AutoDarkModeApp.Controls;
using AutoDarkModeLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Windows.System.Power;
using AdmProperties = AutoDarkModeLib.Properties;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageSwitchModes.xaml
    /// </summary>
    public partial class PageSwitchModes : Page
    {
        private readonly bool init = true;
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private PageSwitchModesViewModel ViewModel = new();

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

            CheckBoxIdleTimer.IsChecked = builder.Config.IdleChecker.Enabled;
            if (builder.Config.IdleChecker.Enabled)
            {
                StackPanelIdleTimer.Visibility = Visibility.Visible;
            }
            else
            {
                StackPanelIdleTimer.Visibility = Visibility.Collapsed;
            }

            CheckBoxAutoSwitchNotify.IsChecked = builder.Config.AutoSwitchNotify.Enabled;
            if (builder.Config.AutoSwitchNotify.Enabled)
            {
                StackPanelAutoSwitchNotify.Visibility = Visibility.Visible;
            }
            else
            {
                StackPanelAutoSwitchNotify.Visibility = Visibility.Collapsed;
            }

            ComboBoxGPUSamples.SelectedIndex = builder.Config.GPUMonitoring.Samples - 1;
            try
            {
                NumberBoxAutoSwitchNotifyGracePeriod.Value = Convert.ToDouble(builder.Config.AutoSwitchNotify.GracePeriodMinutes);
                NumberBoxGPUThreshold.Value = Convert.ToDouble(builder.Config.GPUMonitoring.Threshold);
                NumberBoxIdleTimer.Value = Convert.ToDouble(builder.Config.IdleChecker.Threshold);
            }
            catch
            {
                NumberBoxAutoSwitchNotifyGracePeriod.Value = 2;
                NumberBoxGPUThreshold.Value = 30;
                NumberBoxIdleTimer.Value = 5;
            }

            CheckBoxBatteryDarkMode.IsChecked = builder.Config.Events.DarkThemeOnBattery;

            HotkeyTextboxForceDark.Text = builder.Config.Hotkeys.ForceDark ?? "";
            HotkeyTextboxForceLight.Text = builder.Config.Hotkeys.ForceLight ?? "";
            HotkeyTextboxNoForce.Text = builder.Config.Hotkeys.NoForce ?? "";
            HotkeyTextboxToggleAutomaticThemeSwitch.Text = builder.Config.Hotkeys.ToggleAutoThemeSwitch ?? "";
            HotkeyTextboxToggleTheme.Text = builder.Config.Hotkeys.ToggleTheme ?? "";
            HotkeyTextboxTogglePostpone.Text = builder.Config.Hotkeys.TogglePostpone ?? "";
            HotkeyCheckboxToggleAutomaticThemeSwitchNotification.IsChecked = builder.Config.Notifications.OnAutoThemeSwitching;
            HotkeyCheckboxTogglePostpone.IsChecked = builder.Config.Notifications.OnSkipNextSwitch;


            ToggleHotkeys.IsOn = builder.Config.Hotkeys.Enabled;

            SimpleStackPanelProcessBlockList.Visibility =
                builder.Config.ProcessBlockList.Enabled ? Visibility.Visible : Visibility.Collapsed;
            BlockListOptionsSeparator.Visibility =
                builder.Config.ProcessBlockList.Enabled ? Visibility.Visible : Visibility.Collapsed;
            CheckBoxBlockList.IsChecked = builder.Config.ProcessBlockList.Enabled;
            ItemsControlProcessBlockList.ItemsSource = builder.Config.ProcessBlockList.ProcessNames;
            DataContext = ViewModel;
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

        private void CheckBoxIdleTimer_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxIdleTimer.IsChecked.Value)
            {
                StackPanelIdleTimer.Visibility = Visibility.Visible;
                builder.Config.IdleChecker.Enabled = true;
            }
            else
            {
                StackPanelIdleTimer.Visibility = Visibility.Collapsed;
                builder.Config.IdleChecker.Enabled = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxIdleTimer_Click");
            }
        }

        private void ShowErrorMessage(Exception ex, string location)
        {
            string error = AdmProperties.Resources.ErrorMessageBox + $"\nError ocurred in: {location}" + ex.Source + "\n" + ex.Message;
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


                try
                {
                    builder.Config.GPUMonitoring.Threshold = Convert.ToInt32(NumberBoxGPUThreshold.Value);
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex, "NumberBoxGPUThreshold_ValueChanged");
                }
            }
        }

        private void NumberBoxIdleTimer_ValueChanged(ModernWpf.Controls.NumberBox sender, ModernWpf.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!init)
            {
                if (double.IsNaN(NumberBoxIdleTimer.Value)) //fixes crash when leaving box empty and clicking outside it
                    return;

                try
                {
                    builder.Config.IdleChecker.Threshold = Convert.ToInt32(NumberBoxIdleTimer.Value);
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex, "NumberBoxGPUThreshold_ValueChanged");
                }
            }
        }

        private void CheckBoxAutoSwitchNotify_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxAutoSwitchNotify.IsChecked.Value)
            {
                builder.Config.AutoSwitchNotify.Enabled = true;
                StackPanelAutoSwitchNotify.Visibility = Visibility.Visible;
            }
            else
            {
                builder.Config.AutoSwitchNotify.Enabled = false;
                StackPanelAutoSwitchNotify.Visibility = Visibility.Collapsed;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxAutoSwitchNotifyClick");
            }
        }

        private void NumberBoxAutoSwitchNotifyGracePeriod_ValueChanged(ModernWpf.Controls.NumberBox sender, ModernWpf.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!init)
            {
                if (double.IsNaN(NumberBoxAutoSwitchNotifyGracePeriod.Value)) //fixes crash when leaving box empty and clicking outside it
                    return;

                try
                {
                    builder.Config.AutoSwitchNotify.GracePeriodMinutes = Convert.ToInt32(NumberBoxAutoSwitchNotifyGracePeriod.Value);
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex, "NumberBoxAutoSwitchNotifyGracePeriod_ValueChanged");
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
            if (hotkeyString == builder.Config.Hotkeys.NoForce) return;
            builder.Config.Hotkeys.NoForce = hotkeyString;
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
            if (hotkeyString == builder.Config.Hotkeys.ForceDark) return;
            builder.Config.Hotkeys.ForceDark = hotkeyString;
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
            if (hotkeyString == builder.Config.Hotkeys.ForceLight) return;
            builder.Config.Hotkeys.ForceLight = hotkeyString;
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
            Key key = e.SystemKey == Key.None ? e.Key : e.SystemKey;
            string keyString = key.ToString();

            //Trace.WriteLine(e.SystemKey);
            Trace.WriteLine(keyString);

            if (keyString.Contains("Alt") || keyString.Contains("Shift") || keyString.Contains("Win") || keyString.Contains("Ctrl") || keyString.Contains("System"))
            {
                return null;
            }

            ModifierKeys modifiers = Keyboard.Modifiers;
            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            {
                modifiers |= ModifierKeys.Windows;
            }

            if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                modifiers |= ModifierKeys.Alt;
            }

            string isShift = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? "Shift + " : "";
            string isCtrl = (modifiers & ModifierKeys.Control) == ModifierKeys.Control ? "Ctrl + " : "";
            string isWin = (modifiers & ModifierKeys.Windows) == ModifierKeys.Windows ? "LWin + " : "";
            string isAlt = (modifiers & ModifierKeys.Alt) == ModifierKeys.Alt ? "Alt + " : "";
            string modifiersString = $"{isCtrl}{isShift}{isAlt}{isWin}";
            return modifiersString.Length > 0 ? $"{modifiersString}{key}" : null;
        }

        private void HotkeyTextToggleAutomaticThemeSwitch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string hotkeyString = GetHotkeyString(e);
            if (sender is TextBox tb)
            {
                tb.Text = hotkeyString;
            }
            if (hotkeyString == builder.Config.Hotkeys.ToggleAutoThemeSwitch) return;
            builder.Config.Hotkeys.ToggleAutoThemeSwitch = hotkeyString;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "hotkeybox_toggletheme");
            }
        }

        private void HotkeyCheckboxToggleAutomaticThemeSwitchNotification_Click(object sender, RoutedEventArgs e)
        {
            if (HotkeyCheckboxToggleAutomaticThemeSwitchNotification.IsChecked.Value)
            {
                builder.Config.Notifications.OnAutoThemeSwitching = true;
            }
            else
            {
                builder.Config.Notifications.OnAutoThemeSwitching = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "HotkeyCheckboxToggleAutomaticThemeSwitchNotification_Click");
            }
        }

        private void HotkeyTextboxToggleTheme_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string hotkeyString = GetHotkeyString(e);
            if (sender is TextBox tb)
            {
                tb.Text = hotkeyString;
            }
            if (hotkeyString == builder.Config.Hotkeys.ToggleTheme) return;
            builder.Config.Hotkeys.ToggleTheme = hotkeyString;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "hotkeybox_toggletheme");
            }
        }

        private void HotkeyTextboxTogglePostpone_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string hotkeyString = GetHotkeyString(e);
            if (sender is TextBox tb)
            {
                tb.Text = hotkeyString;
            }
            if (hotkeyString == builder.Config.Hotkeys.TogglePostpone) return;
            builder.Config.Hotkeys.TogglePostpone = hotkeyString;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "hotkeybox_togglepostpone");
            }
        }

        private void HotkeyCheckboxTogglePostpone_Click(object sender, RoutedEventArgs e)
        {
            if (HotkeyCheckboxTogglePostpone.IsChecked.Value)
            {
                builder.Config.Notifications.OnSkipNextSwitch = true;
            }
            else
            {
                builder.Config.Notifications.OnSkipNextSwitch  = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "HotkeyCheckboxToggleAutomaticThemeSwitchNotification_Click");
            }
        }

        private void SwitchModesAddSelectedProcess_OnClick(object sender, RoutedEventArgs e)
        {
            if (ComboBoxProcessBlockList.SelectedItem is not string processName ||
                builder.Config.ProcessBlockList.ProcessNames.Contains(processName)) return;

            builder.Config.ProcessBlockList.ProcessNames.Add(processName);
            try
            {
                builder.Save();
                ItemsControlProcessBlockList.Items.Refresh();
                ComboBoxProcessBlockList.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "SwitchModesAddSelectedProcess_OnClick");
            }
        }

        private void SwitchModesRemoveProcess_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string entry })
            {
                var result = builder.Config.ProcessBlockList.ProcessNames.Remove(entry);
                if (result)
                {
                    ItemsControlProcessBlockList.Items.Refresh();
                    builder.Save();
                }
            }
        }

        private void CheckBoxProcessBlockList_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.ProcessBlockList.Enabled = !builder.Config.ProcessBlockList.Enabled;
            CheckBoxBlockList.IsChecked = builder.Config.ProcessBlockList.Enabled;
            BlockListOptionsSeparator.Visibility =
                builder.Config.ProcessBlockList.Enabled ? Visibility.Visible : Visibility.Collapsed;
            SimpleStackPanelProcessBlockList.Visibility =
                builder.Config.ProcessBlockList.Enabled ? Visibility.Visible : Visibility.Collapsed;
            builder.Save();
        }

        private void ComboBoxProcessBlockList_DropDownOpened(object sender, EventArgs e)
        {
            var processes = Process.GetProcesses();
            Task.Run(() => BuildProcessList(processes));
        }

        private void BuildProcessList(Process[] processes)
        {
            bool isEmpty = ViewModel.FilteredProcesses.Count == 0;
            SortedSet<string> filteredProcesses = new();
            foreach (var process in processes)
            {
                try
                {
                    if (process.MainWindowHandle == -0) continue;

                    // No point in showing a process' name in the dropdown if it's already being excluded out
                    if (!builder.Config.ProcessBlockList.ProcessNames.Contains(process.ProcessName))
                    {
                        if (isEmpty)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (!ViewModel.FilteredProcesses.Contains(process.ProcessName))
                                {
                                    ViewModel.FilteredProcesses.Add(process.ProcessName);
                                    var sorted = ViewModel.FilteredProcesses.OrderBy(i => i);
                                    ViewModel.FilteredProcesses = new(sorted);
                                }
                            });
                        }
                        else
                        {
                            filteredProcesses.Add(process.ProcessName);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => ShowErrorMessage(ex, "RefreshProcessComboBox"));
                }
            }
            if (!isEmpty)
            {
                Dispatcher.Invoke(() =>
                {
                    ViewModel.FilteredProcesses.Clear();
                    foreach (var process in filteredProcesses)
                    {
                        ViewModel.FilteredProcesses.Add(process);
                    }
                });
            }
        }
    }
}
