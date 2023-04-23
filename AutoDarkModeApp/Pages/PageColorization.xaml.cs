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
using ModernWpf.Media.Animation;
using System;
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

        public PageColorization()
        {
            InitializeComponent();

            // todo get from backend
            if (builder.Config.ColorizationSwitch.Component.LightHex.Length == 0)
            {
                builder.Config.ColorizationSwitch.Component.LightHex = "#FF000000";
            }
            if (builder.Config.ColorizationSwitch.Component.DarkHex.Length == 0)
            {
                builder.Config.ColorizationSwitch.Component.DarkHex = "#FF000000";
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
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "ColorizationPage", "Your hex strings are invalid, please set correct hex strings");
            }

            lightColorizationColor.A = 255;
            darkColorizationColor.A = 255;

            ColorControlsLight.InitialColorBrush = new(lightColorizationColor);
            ColorControlsLight.SelectedColorBrush = new(lightColorizationColor);
            ColorControlsDark.InitialColorBrush = new(darkColorizationColor);
            ColorControlsDark.SelectedColorBrush = new(darkColorizationColor);

            if (!builder.Config.ColorizationSwitch.Component.LightAutoColorization)
            {
                LightExpander.IsExpanded = true;
                LightAutoColorizationComboBox.SelectedItem = ManualLight;
            }
            if (!builder.Config.ColorizationSwitch.Component.DarkAutoColorization)
            {
                DarkExpander.IsExpanded = true;
                DarkAutoColorizationComboBox.SelectedItem = ManualDark;
            }

            if (builder.Config.ColorizationSwitch.Enabled)
            {
                ToggleSwitchColorizationEnabled.IsOn = true;
            }
            else
            {
                ToggleSwitchColorizationEnabled.IsOn = true;
            }
        }

        private void TextBlockBackButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Frame.Navigate(typeof(PagePersonalization), null, new DrillInNavigationTransitionInfo());
        }

        private void ToggleSwitchAutoColorization_Toggled(object sender, RoutedEventArgs e)
        {

        }

        private void darkColorizationSetBox_ColorChanged(object sender, ColorChangedEventArgs e)
        {

        }

        private void lightColorizationSetBox_ColorChanged(object sender, ColorChangedEventArgs e)
        {

        }
    }
}