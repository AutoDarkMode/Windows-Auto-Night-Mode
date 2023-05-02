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
using AutoDarkModeApp.Handlers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using ModernWpf.Media.Animation;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static AutoDarkModeApp.Controls.ColorControlPanel;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageColorization.xaml
    /// </summary>
    public partial class PageColorization : ModernWpf.Controls.Page
    {
        public ColorControlPanel ColorControlsLight
        { get { return lightColorizationSetBox; } }
        public ColorControlPanel ColorControlsDark
        { get { return darkColorizationSetBox; } }
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private readonly bool init = true;

        public PageColorization()
        {
            InitializeComponent();

            if (!builder.Config.ColorizationSwitch.Component.LightAutoColorization)
            {
                LightExpander.IsExpanded = true;
                LightAutoColorizationComboBox.SelectedItem = ManualLight;
            }
            else
            {
                LightColorPickerStackPanel.IsEnabled = false;
            }


            if (!builder.Config.ColorizationSwitch.Component.DarkAutoColorization)
            {
                DarkExpander.IsExpanded = true;
                DarkAutoColorizationComboBox.SelectedItem = ManualDark;
            }
            else
            {
                DarkColorPickerStackPanel.IsEnabled = false;
            }

            if (builder.Config.ColorizationSwitch.Enabled)
            {
                ToggleSwitchColorizationEnabled.IsOn = true;
            }
            else
            {
                ToggleSwitchColorizationEnabled.IsOn = false;
            }
            init = false;
        }

        private void InitializeColors()
        {
            var Component = builder.Config.ColorizationSwitch.Component;


            string messageRaw = MessageHandler.Client.SendMessageAndGetReply(Command.GetCurrentColorization);
            ApiResponse response = ApiResponse.FromString(messageRaw);
            if (response.StatusCode == StatusCode.Ok)
            {

                // if hexes are undefined pre-load them for the user
                bool needsSave = false;
                if (Component.LightHex.Length == 0)
                {
                    Component.LightHex = response.Message;
                    needsSave = true;
                }
                if (Component.DarkHex.Length == 0)
                {
                    Component.DarkHex = response.Message;
                    needsSave = true;
                }
                // if auto colorization is enabled, update the hex to the respective current colorization color
                if (Component.LightAutoColorization || Component.DarkAutoColorization)
                {
                    Theme requestedTheme = Theme.Unknown;
                    try
                    {
                        requestedTheme = (Theme)Enum.Parse(typeof(Theme), response.Details);
                        if (Component.LightAutoColorization && requestedTheme == Theme.Light)
                        {
                            Component.LightHex = response.Message;
                            needsSave = true;

                        }
                        if (Component.DarkAutoColorization && requestedTheme == Theme.Dark)
                        {
                            Component.DarkHex = response.Message;
                            needsSave = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageColorization_SaveDefaultColorization_ParseRequestedTheme");
                    }

                }

                if (needsSave)
                {
                    try
                    {
                        builder.Save();
                    }
                    catch (Exception ex)
                    {
                        ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageColorization_SaveDefaultColorization");
                    }
                }
            }
            else
            {
                ErrorMessageBoxes.ShowErrorMessageFromApi(response, Window.GetWindow(this));

                if (Component.LightHex.Length == 0)
                {
                    Component.LightHex = "#C40078D4";
                }
                if (Component.DarkHex.Length == 0)
                {
                    Component.DarkHex = "#C40078D4";
                }

                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageColorization_SaveDefaultColorization");
                }
            }

            Color lightColorizationColor = Color.FromRgb(0, 0, 0);
            Color darkColorizationColor = Color.FromRgb(0, 0, 0);
            try
            {
                lightColorizationColor = (Color)ColorConverter.ConvertFromString(builder.Config.ColorizationSwitch.Component.LightHex);
                darkColorizationColor = (Color)ColorConverter.ConvertFromString(builder.Config.ColorizationSwitch.Component.DarkHex);
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "ColorizationPage", "Hex strings are invalid");
            }

            lightColorizationColor.A = 255;
            darkColorizationColor.A = 255;

            ColorControlsLight.InitialColorBrush = new(lightColorizationColor);
            ColorControlsLight.SelectedColorBrush = new(lightColorizationColor);
            ColorControlsDark.InitialColorBrush = new(darkColorizationColor);
            ColorControlsDark.SelectedColorBrush = new(darkColorizationColor);
        }

        private void TextBlockBackButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Frame.Navigate(typeof(PagePersonalization), null, new DrillInNavigationTransitionInfo());
        }

        private async void ToggleSwitchColorizationEnabled_Toggled(object sender, RoutedEventArgs e)
        {
            if (init) return;
            builder.Config.ColorizationSwitch.Enabled = ToggleSwitchColorizationEnabled.IsOn;

            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageColorization_SaveBackendColorization");
            }

            if (ToggleSwitchColorizationEnabled.IsOn)
            {
                await RequestSwitch();
                Dispatcher.Invoke(InitializeColors);
            }
        }

        private void DarkColorizationSetBox_ColorChanged(object sender, ColorChangedEventArgs e)
        {
            builder.Config.ColorizationSwitch.Component.DarkHex = e.CurrentColor.ToString();
        }

        private void LightColorizationSetBox_ColorChanged(object sender, ColorChangedEventArgs e)
        {
            builder.Config.ColorizationSwitch.Component.LightHex = e.CurrentColor.ToString();
        }

        private async void DarkColorizationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                darkColorizationSetBox.InitialColorBrush = new((Color)ColorConverter.ConvertFromString(builder.Config.ColorizationSwitch.Component.DarkHex));
                builder.Save();
                await RequestSwitch();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "Colorization_SetLight");
            }
        }

        private async void LightColorizationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lightColorizationSetBox.InitialColorBrush = new((Color)ColorConverter.ConvertFromString(builder.Config.ColorizationSwitch.Component.LightHex));
                builder.Save();
                await RequestSwitch();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "Colorization_SetLight");
            }
        }

        private async void LightAutoColorizationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (init) return;
            if (LightAutoColorizationComboBox.SelectedItem == AutoLight)
            {
                builder.Config.ColorizationSwitch.Component.LightAutoColorization = true;
                LightColorPickerStackPanel.IsEnabled = false;
            }
            else
            {
                builder.Config.ColorizationSwitch.Component.LightAutoColorization = false;
                LightColorPickerStackPanel.IsEnabled = true;
                LightExpander.IsExpanded = true;
            }

            try
            {
                builder.Save();
                var brush = ColorControlsLight.InitialColorBrush;
                await RequestSwitch();
                if (builder.Config.ColorizationSwitch.Component.LightAutoColorization)
                {
                    Debug.WriteLine("waiting for colorization change");
                    await WaitForColorizationChange(builder.Config.ColorizationSwitch.Component.LightHex.ToLower(), 5);
                    Dispatcher.Invoke(InitializeColors);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "Colorization_ToggleAutomaticLight");
            }
        }

        private async void DarkAutoColorizationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (init) return;

            if (DarkAutoColorizationComboBox.SelectedItem == AutoDark)
            {
                builder.Config.ColorizationSwitch.Component.DarkAutoColorization = true;
                DarkColorPickerStackPanel.IsEnabled = false;
            }
            else
            {
                builder.Config.ColorizationSwitch.Component.DarkAutoColorization = false;
                DarkColorPickerStackPanel.IsEnabled = true;
                DarkExpander.IsExpanded = true;
            }

            try
            {
                builder.Save();
                await RequestSwitch();
                if (builder.Config.ColorizationSwitch.Component.DarkAutoColorization)
                {
                    Debug.WriteLine("waiting for colorization change");
                    await WaitForColorizationChange(builder.Config.ColorizationSwitch.Component.DarkHex.ToLower(), 5);
                    Dispatcher.Invoke(InitializeColors);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "Colorization_ToggleAutomaticDark");
            }

        }

        private async Task WaitForColorizationChange(string initial, int timeout)
        {
            if (!builder.Config.ColorizationSwitch.Enabled) return;
            int tries = 0;
            while (tries < timeout)
            {
                string messageRaw = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.GetCurrentColorization);
                ApiResponse response = ApiResponse.FromString(messageRaw);
                if (response.StatusCode == StatusCode.Ok)
                {
                    if (response.Message.ToLower() != initial)
                    {
                        Debug.WriteLine("colorization change detected, updating UI");
                        Debug.WriteLine(response.Message);
                        break;
                    }
                    else
                    {
                        await Task.Delay(1000);
                        tries++;
                    }
                }
            }
        }

        private static async Task RequestSwitch()
        {
            string result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException("Api " + result, "PageTime");
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeColors();
        }
    }
}