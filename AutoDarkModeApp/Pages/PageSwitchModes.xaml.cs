using AutoDarkModeApp.Properties;
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
using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using AutoDarkModeComms;
using AutoDarkModeApp.Handlers;
using Windows.System.Power;
using System.Diagnostics;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageSwitchModes.xaml
    /// </summary>
    public partial class PageSwitchModes : Page
    {
        private bool init = true;
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        readonly ICommandClient messagingClient = new ZeroMQClient(Address.DefaultPort);

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
            ComboBoxGPUSamples.SelectedIndex = builder.Config.GPUMonitoring.Samples-1;
            NumberBoxGPUThreshold.Value = Convert.ToDouble(builder.Config.GPUMonitoring.Threshold);

            CheckBoxBatteryDarkMode.IsChecked = builder.Config.Events.DarkThemeOnBattery;

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
            string error = Properties.Resources.errorThemeApply + $"\n\nError ocurred in: {location}" + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
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
            if (!init)
            {
                builder.Config.GPUMonitoring.Samples = ComboBoxGPUSamples.SelectedIndex + 1;

                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex, "ComboBoxGPUSamples_DropDownClosed");
                }
            }
        }
    }
}
