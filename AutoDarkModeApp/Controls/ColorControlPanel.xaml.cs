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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AutoDarkModeApp.Controls
{
    /// <summary>
    /// Interaction logic for ColorControlPanel.xaml
    /// </summary>
    public partial class ColorControlPanel : UserControl
    {
        public static readonly DependencyProperty ShowAlphaProperty = DependencyProperty.Register("ShowAlpha", typeof(bool), typeof(ColorControlPanel), new FrameworkPropertyMetadata(ShowAlphaChanged));

        public bool ShowAlpha
        {
            get { return (bool)GetValue(ShowAlphaProperty); }
            set { SetValue(ShowAlphaProperty, value); }
        }
        private bool ThumbsInitialised = false;
        private bool isDragging = false;

        private enum Sliders { A, R, G, B, H, SV, nil }
        private Sliders DrivingSlider = Sliders.nil;

        private Color iniColor = Colors.Black;
        private Color tmpColor = Colors.Black;
        private Color outColor = Colors.Black;
        public Color SelectedColor { get { return outColor; } }

        private double outColorH, outColorS, outColorV;
        private bool useHSV = false;

        private Color thumbRColor = Color.FromRgb(0, 0, 0);
        private Color thumbGColor = Color.FromRgb(0, 0, 0);
        private Color thumbBColor = Color.FromRgb(0, 0, 0);
        private Color thumbHColor = Color.FromRgb(0, 0, 0);

        private SolidColorBrush iniColorBrush = new SolidColorBrush();
        private SolidColorBrush outColorBrush = new SolidColorBrush();
        private SolidColorBrush thumbSVbrush = new SolidColorBrush();
        private SolidColorBrush thumbRbrush;
        private SolidColorBrush thumbGbrush;
        private SolidColorBrush thumbBbrush;
        private SolidColorBrush thumbHbrush;

        private LinearGradientBrush SaturationGradBrush;
        private LinearGradientBrush RgradBrush;
        private LinearGradientBrush GgardBrush;
        private LinearGradientBrush BgradBrush;
        private LinearGradientBrush AgradBrush;

        private static void ShowAlphaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorControlPanel ccp = (ColorControlPanel)d;
            bool b = (bool)e.NewValue;
            ccp.SetValue(ShowAlphaProperty, b);
            if (b)
            {
                ccp.dockAlpha.Visibility= Visibility.Visible;
            }
            else
            {
                ccp.dockAlpha.Visibility= Visibility.Collapsed;
            }
        }

        private void IniGradientBrushes()
        {
            SaturationGradBrush = SaturationGradient.Background as LinearGradientBrush;

            RgradBrush = sliderRed.Background as LinearGradientBrush;
            GgardBrush = sliderGreen.Background as LinearGradientBrush;
            BgradBrush = sliderBlue.Background as LinearGradientBrush;
            AgradBrush = sliderAlpha.Background as LinearGradientBrush;
        }

        private void IniThumbsBrushes()
        {
            thumbRbrush = sliderRed.Foreground as SolidColorBrush;
            thumbGbrush = sliderGreen.Foreground as SolidColorBrush;
            thumbBbrush = sliderBlue.Foreground as SolidColorBrush;

            thumbHbrush = sliderSpectrum.Foreground as SolidColorBrush;
            //REMOVED
            //thumbSV.Fill = thumbSVbrush;

            ThumbsInitialised = true;
        }

        private void IniThumbs(object sender, RoutedEventArgs e)
        {
            RootGrid.Loaded -= IniThumbs;

            if (!RootGrid.IsVisible) RootGrid.LayoutUpdated += IniThumbsDeferred;
            else AdjustThumbs(SelectedColorBrush.Color);
        }

        private void IniThumbsDeferred(object sender, EventArgs e)
        {
            if (RootGrid.ActualHeight != 0 && RootGrid.ActualWidth != 0)
            {
                RootGrid.LayoutUpdated -= IniThumbsDeferred;

                AdjustThumbs(SelectedColorBrush.Color);
            }
        }

        // Algorithm taken from Wikipedia https://en.wikipedia.org/wiki/HSL_and_HSV
        public static void ConvertRgbToHsv(Color color, out double hue, out double saturation, out double value)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double chroma, min = r, max = r;

            if (g > max) max = g;
            else if (g < min) min = g;

            if (b > max) max = b;
            else if (b < min) min = b;

            value = max;
            chroma = max - min;

            if (value == 0) saturation = 0;
            else saturation = chroma / max;

            if (saturation == 0) hue = 0;
            else if (max == r) hue = (g - b) / chroma;
            else if (max == g) hue = 2 + (b - r) / chroma;
            else hue = 4 + (r - g) / chroma;

            hue *= 60;
            if (hue < 0) hue += 360;
        }

        // Algorithm taken from Wikipedia https://en.wikipedia.org/wiki/HSL_and_HSV
        public static Color ConvertHsvToRgb(double hue, double saturation, double value)
        {
            double chroma = value * saturation;

            if (hue == 360) hue = 0;

            double hueTag = hue / 60;
            double x = chroma * (1 - Math.Abs(hueTag % 2 - 1));
            double m = value - chroma;

            double R, G, B;

            switch ((int)hueTag)
            {
                case 0:
                    R = chroma; G = x; B = 0;
                    break;
                case 1:
                    R = x; G = chroma; B = 0;
                    break;
                case 2:
                    R = 0; G = chroma; B = x;
                    break;
                case 3:
                    R = 0; G = x; B = chroma;
                    break;
                case 4:
                    R = x; G = 0; B = chroma;
                    break;
                default:
                    R = chroma; G = 0; B = x;
                    break;
            }

            R += m; G += m; B += m;
            R *= 255; G *= 255; B *= 255;

            return Color.FromRgb((byte)R, (byte)G, (byte)B);
        }

        public static double GetBrightness(byte R, byte G, byte B)
        {
            // Value = Max(R,G,B)
            byte max = R;

            if (G > max) max = G;
            if (B > max) max = B;

            return max / 255.0;
        }

        public static double GetSaturation(byte R, byte G, byte B)
        {
            double r = R / 255.0;
            double g = G / 255.0;
            double b = B / 255.0;

            double chroma, value, saturation;
            double min = r, max = r;

            if (g > max) max = g;
            else if (g < min) min = g;

            if (b > max) max = b;
            else if (b < min) min = b;

            value = max;
            chroma = max - min;

            if (value == 0) saturation = 0;
            else saturation = chroma / max;

            return saturation;
        }

        private bool ColorCodeParser(string hexcode, out Color color)
        {
            color = Color.FromArgb(0, 0, 0, 0);
            bool success = false;

            if (!string.IsNullOrWhiteSpace(hexcode.Trim()))
            {
                if (hexcode.Substring(0, 1) == "#") hexcode = hexcode.Substring(1);

                if (hexcode.Length == 6)
                {
                    hexcode = "FF" + hexcode;
                }

                if (hexcode.Length == 8)
                {
                    byte numeric;
                    string strByte;

                    // Alpha
                    strByte = hexcode.Substring(0, 2);

                    if (!byte.TryParse(strByte, NumberStyles.HexNumber, null as IFormatProvider, out numeric)) return false; // >>> FAILED >>>
                    color.A = numeric;

                    // Red
                    strByte = hexcode.Substring(2, 2);

                    if (!byte.TryParse(strByte, NumberStyles.HexNumber, null as IFormatProvider, out numeric)) return false; // >>> FAILED >>>
                    color.R = numeric;

                    // Green
                    strByte = hexcode.Substring(4, 2);

                    if (!byte.TryParse(strByte, NumberStyles.HexNumber, null as IFormatProvider, out numeric)) return false; // >>> FAILED >>>
                    color.G = numeric;

                    // Blue
                    strByte = hexcode.Substring(6, 2);

                    if (!byte.TryParse(strByte, NumberStyles.HexNumber, null as IFormatProvider, out numeric)) return false; // >>> FAILED >>>
                    color.B = numeric;

                    success = true;
                }
            }

            return success;
        }

        private void InputComponent(TextBox inputBox)
        {
            string input = inputBox.Text;
            byte numeric;

            switch (inputBox.Name)
            {
                case "txtAvalue":
                    if (byte.TryParse(input, out numeric) && outColor.A != numeric)
                    {
                        outColor.A = numeric;
                        outColorBrush.Color = outColor;
                    }
                    else txtAvalue.Text = outColor.A.ToString();

                    break;

                case "txtRvalue":
                    if (byte.TryParse(input, out numeric) && outColor.R != numeric)
                    {
                        outColor.R = numeric;
                        outColorBrush.Color = outColor;
                    }
                    else txtRvalue.Text = outColor.R.ToString();

                    break;

                case "txtGvalue":
                    if (byte.TryParse(input, out numeric) && outColor.G != numeric)
                    {
                        outColor.G = numeric;
                        outColorBrush.Color = outColor;
                    }
                    else txtGvalue.Text = outColor.G.ToString();

                    break;

                case "txtBvalue":
                    if (byte.TryParse(input, out numeric) && outColor.B != numeric)
                    {
                        outColor.B = numeric;
                        outColorBrush.Color = outColor;
                    }
                    else txtBvalue.Text = outColor.B.ToString();

                    break;

                case "txtColorCode":
                    Color buffColor;
                    input = input.Trim();
                    if (ColorCodeParser(input, out buffColor) && outColor != buffColor)
                    {
                        outColorBrush.Color = buffColor;
                    }
                    else txtColorCode.Text = outColor.ToString();

                    break;
            }
        }

        private void AdjustThumbs(Color theColor)
        {
            // --- ARGB ---
            byte A = theColor.A;
            byte R = theColor.R;
            byte G = theColor.G;
            byte B = theColor.B;

            outColor = theColor;
            txtColorCode.Text = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", A, R, G, B);

            // Alpha
            if (DrivingSlider != Sliders.A) sliderAlpha.Value = A;
            txtAvalue.Text = A.ToString();

            theColor.A = 0;
            AgradBrush.GradientStops[0].Color = theColor;
            theColor.A = 255;
            AgradBrush.GradientStops[1].Color = theColor;
            //theColor.A = A; the alpha will be setted after the SV thumb adjusting

            // Red
            if (DrivingSlider != Sliders.R) sliderRed.Value = R;
            txtRvalue.Text = R.ToString();

            theColor.R = 0;
            RgradBrush.GradientStops[0].Color = theColor;
            theColor.R = 255;
            RgradBrush.GradientStops[1].Color = theColor;
            theColor.R = R;

            thumbRColor.R = R;
            thumbRbrush.Color = thumbRColor;

            // Green
            if (DrivingSlider != Sliders.G) sliderGreen.Value = G;
            txtGvalue.Text = G.ToString();

            theColor.G = 0;
            GgardBrush.GradientStops[0].Color = theColor;
            theColor.G = 255;
            GgardBrush.GradientStops[1].Color = theColor;
            theColor.G = G;

            thumbGColor.G = G;
            thumbGbrush.Color = thumbGColor;

            // Blue
            if (DrivingSlider != Sliders.B) sliderBlue.Value = B;
            txtBvalue.Text = B.ToString();

            theColor.B = 0;
            BgradBrush.GradientStops[0].Color = theColor;
            theColor.B = 255;
            BgradBrush.GradientStops[1].Color = theColor;
            theColor.B = B;

            thumbBColor.B = B;
            thumbBbrush.Color = thumbBColor;

            // --- HSV ---
            ConvertRgbToHsv(theColor, out outColorH, out outColorS, out outColorV);

            thumbHColor = ConvertHsvToRgb(outColorH, 1, 1);

            // Hue
            thumbHbrush.Color = thumbHColor;
            if (DrivingSlider != Sliders.H) sliderSpectrum.Value = outColorH;

            // SV thumb
            // REMOVED
            //thumbSVbrush.Color = theColor;

            // Saturation gradient
            SaturationGradBrush.GradientStops[1].Color = thumbHColor;

            // Saturation and value to canvas coords
            Canvas.SetLeft(thumbSV, outColorS * SaturationGradient.ActualWidth - 0.5 * thumbSV.ActualWidth);
            Canvas.SetTop(thumbSV, (1 - outColorV) * SaturationGradient.ActualHeight - 0.5 * thumbSV.ActualHeight);

            // RISE EVENT
            if (tmpColor != outColor)
            {
                ColorChanged?.Invoke(this, new ColorChangedEventArgs(iniColor, tmpColor, outColor));
                tmpColor = outColor;
            }
        }

        private void AdjustThumbs(double H, double S, double V)
        {
            // --- HSV ---
            thumbHColor = ConvertHsvToRgb(H, 1, 1);

            // Hue
            thumbHbrush.Color = thumbHColor;
            if (DrivingSlider != Sliders.H) sliderSpectrum.Value = H;

            // Saturation gradient
            SaturationGradBrush.GradientStops[1].Color = thumbHColor;

            // Saturation and value to canvas coords
            if (DrivingSlider != Sliders.SV)
            {
                Canvas.SetLeft(thumbSV, S * SaturationGradient.ActualWidth - 0.5 * thumbSV.ActualWidth);
                Canvas.SetTop(thumbSV, (1 - V) * SaturationGradient.ActualHeight - 0.5 * thumbSV.ActualHeight);
            }

            byte A = outColor.A;
            //outColor = ConvertHsvToRgb(H, S, V);

            // SV thumb
            thumbSVbrush.Color = outColor;

            // --- ARGB ---
            outColor.A = A;
            byte R = outColor.R;
            byte G = outColor.G;
            byte B = outColor.B;

            txtColorCode.Text = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", A, R, G, B);

            // Alpha
            sliderAlpha.Value = A;
            txtRvalue.Text = A.ToString();

            outColor.A = 0;
            AgradBrush.GradientStops[0].Color = outColor;
            outColor.A = 255;
            AgradBrush.GradientStops[1].Color = outColor;
            //outColor.A = A; // the alpha will be setted after RGB adjusting

            // Red
            sliderRed.Value = R;
            txtRvalue.Text = R.ToString();

            outColor.R = 0;
            RgradBrush.GradientStops[0].Color = outColor;
            outColor.R = 255;
            RgradBrush.GradientStops[1].Color = outColor;
            outColor.R = R;

            thumbRColor.R = R;
            thumbRbrush.Color = thumbRColor;

            // Green
            sliderGreen.Value = G;
            txtGvalue.Text = G.ToString();

            outColor.G = 0;
            GgardBrush.GradientStops[0].Color = outColor;
            outColor.G = 255;
            GgardBrush.GradientStops[1].Color = outColor;
            outColor.G = G;

            thumbGColor.G = G;
            thumbGbrush.Color = thumbGColor;

            // Blue
            sliderBlue.Value = B;
            txtBvalue.Text = B.ToString();

            outColor.B = 0;
            BgradBrush.GradientStops[0].Color = outColor;
            outColor.B = 255;
            BgradBrush.GradientStops[1].Color = outColor;
            outColor.B = B;

            thumbBColor.B = B;
            thumbBbrush.Color = thumbBColor;

            outColor.A = A;

            // RISE EVENT
            if (tmpColor != outColor)
            {
                ColorChanged?.Invoke(this, new ColorChangedEventArgs(iniColor, tmpColor, outColor));
                tmpColor = outColor;
            }
        }

        private void preMLBdown_addSliderHandler(object sender, MouseButtonEventArgs e)
        {
            Slider sourceSlider = e.Source as Slider;

            if (sourceSlider != null && DrivingSlider == Sliders.nil)
            {
                switch (sourceSlider.Name)
                {
                    case "sliderAlpha":
                        sliderAlpha.ValueChanged += AlphaThumbMove;
                        DrivingSlider = Sliders.A;
                        break;

                    case "sliderRed":
                        sliderRed.ValueChanged += RedThumbMove;
                        DrivingSlider = Sliders.R;
                        break;

                    case "sliderGreen":
                        sliderGreen.ValueChanged += GreenThumbMove;
                        DrivingSlider = Sliders.G;
                        break;

                    case "sliderBlue":
                        sliderBlue.ValueChanged += BlueThumbMove;
                        DrivingSlider = Sliders.B;
                        break;

                    case "sliderSpectrum":
                        sliderSpectrum.ValueChanged += HueThumbMove;
                        DrivingSlider = Sliders.H;
                        break;
                }
            }
        }

        private void removeSliderHandler()
        {
            switch (DrivingSlider)
            {
                case Sliders.A:
                    sliderAlpha.ValueChanged -= AlphaThumbMove;
                    break;

                case Sliders.R:
                    sliderRed.ValueChanged -= RedThumbMove;
                    break;

                case Sliders.G:
                    sliderGreen.ValueChanged -= GreenThumbMove;
                    break;

                case Sliders.B:
                    sliderBlue.ValueChanged -= BlueThumbMove;
                    break;

                case Sliders.H:
                    sliderSpectrum.ValueChanged -= HueThumbMove;
                    break;
            }

            DrivingSlider = Sliders.nil;
        }

        private void preMLBup_removeSliderHandler(object sender, MouseButtonEventArgs e)
        {
            Slider sourceSlider = e.Source as Slider;

            if (sourceSlider != null) removeSliderHandler();
        }

        private void LostMouseCapture_removeSliderHandler(object sender, MouseEventArgs e)
        {
            Slider sourceSlider = e.Source as Slider;

            if (sourceSlider != null) removeSliderHandler();
        }

        private void MLBdownOverSVsquare(object sender, MouseButtonEventArgs e)
        {
            DrivingSlider = Sliders.SV;

            SVthumbMove(e.GetPosition(SaturationGradient));

            SaturationGradient.MouseLeftButtonDown -= MLBdownOverSVsquare;
            SaturationGradient.MouseLeftButtonUp += MLBupSVsquare;

            SaturationGradient.MouseMove += SVthumbMove;

            SaturationGradient.CaptureMouse();
        }

        private void MLBupSVsquare(object sender, MouseButtonEventArgs e)
        {
            DrivingSlider = Sliders.nil;

            SaturationGradient.ReleaseMouseCapture();

            SaturationGradient.MouseMove -= SVthumbMove;

            SaturationGradient.MouseLeftButtonDown += MLBdownOverSVsquare;
            SaturationGradient.MouseLeftButtonUp -= MLBupSVsquare;
        }

        private void SVthumbMove(Point point)
        {
            double X = point.X;
            double Y = point.Y;

            if (X < 0) X = 0;
            else if (X > SaturationGradient.ActualWidth) X = SaturationGradient.ActualWidth;

            if (Y < 0) Y = 0;
            else if (Y > SaturationGradient.ActualHeight) Y = SaturationGradient.ActualHeight;

            outColorS = (float)(X / SaturationGradient.ActualWidth);
            outColorV = (float)(1 - Y / SaturationGradient.ActualHeight);

            Canvas.SetLeft(thumbSV, X - 0.5 * thumbSV.ActualWidth);
            Canvas.SetTop(thumbSV, Y - 0.5 * thumbSV.ActualHeight);

            byte alpha = outColor.A;
            outColor = ConvertHsvToRgb(outColorH, outColorS, outColorV);
            outColor.A = alpha;

            if (outColorBrush.Color != outColor) // HSV values may change while RGB values remain the same
            {
                useHSV = true;
                outColorBrush.Color = outColor;
            }
        }

        private void SVthumbMove(object sender, MouseEventArgs e)
        {
            Point point = e.GetPosition(SaturationGradient);
            double X = point.X;
            double Y = point.Y;

            if (X < 0) X = 0;
            else if (X > SaturationGradient.ActualWidth) X = SaturationGradient.ActualWidth;

            if (Y < 0) Y = 0;
            else if (Y > SaturationGradient.ActualHeight) Y = SaturationGradient.ActualHeight;

            outColorS = (float)(X / SaturationGradient.ActualWidth);
            outColorV = (float)(1 - Y / SaturationGradient.ActualHeight);

            Canvas.SetLeft(thumbSV, X - 0.5 * thumbSV.ActualWidth);
            Canvas.SetTop(thumbSV, Y - 0.5 * thumbSV.ActualHeight);

            byte alpha = outColor.A;
            outColor = ConvertHsvToRgb(outColorH, outColorS, outColorV);
            outColor.A = alpha;

            if (outColorBrush.Color != outColor) // HSV values may change while RGB values remain the same
            {
                useHSV = true;
                outColorBrush.Color = outColor;
            }
        }

        private void HueThumbMove(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            outColorH = (float)e.NewValue;

            byte alpha = outColor.A;
            outColor = ConvertHsvToRgb(outColorH, outColorS, outColorV);
            outColor.A = alpha;

            if (outColorBrush.Color != outColor) // HSV values may change while RGB values remain the same
            {
                useHSV = true;
                outColorBrush.Color = outColor;
            }
            else AdjustThumbs(outColorH, outColorS, outColorV);
        }

        private void RedThumbMove(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            outColor.R = (byte)e.NewValue;
            outColorBrush.Color = outColor;
        }

        private void GreenThumbMove(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            outColor.G = (byte)e.NewValue;
            outColorBrush.Color = outColor;
        }

        private void BlueThumbMove(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            outColor.B = (byte)e.NewValue;
            outColorBrush.Color = outColor;
        }

        private void AlphaThumbMove(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            outColor.A = (byte)e.NewValue;
            outColorBrush.Color = outColor;
        }

        private void LostKeyFocus_RGBApanel(object sender, RoutedEventArgs e)
        {
            TextBox inputBox = e.Source as TextBox;

            if (inputBox != null)
            {
                InputComponent(inputBox);
                e.Handled = true;
            }
        }

        private void KeyDown_RGBApanel(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox inputBox = e.Source as TextBox;

                if (inputBox != null)
                {
                    InputComponent(inputBox);
                    e.Handled = true;
                }
            }
        }

        private void RevertIniColor(object sender, MouseEventArgs e)
        {
            outColor = iniColor;
            outColorBrush.Color = outColor;
        }

        public class ColorChangedEventArgs : EventArgs
        {
            private Color iniColor;
            private Color previousColor;
            private Color currentColor;

            public Color InitialColor { get { return iniColor; } }
            public Color PreviousColor { get { return previousColor; } }
            public Color CurrentColor { get { return currentColor; } }

            public ColorChangedEventArgs(Color iniC, Color preC, Color curC)
            {
                iniColor = iniC;
                previousColor = preC;
                currentColor = curC;
            }
        }

        public event EventHandler<ColorChangedEventArgs> ColorChanged;

        public ColorControlPanel()
        {
            InitializeComponent();

            IniGradientBrushes();
            IniThumbsBrushes();

            rectInitialColor.Background = iniColorBrush;
            rectSelectedColor.Background = outColorBrush;

            RootGrid.Loaded += IniThumbs;

            // Subscribe on events
            SaturationGradient.MouseLeftButtonDown += MLBdownOverSVsquare;

            RootGrid.PreviewMouseLeftButtonDown += preMLBdown_addSliderHandler;
            RootGrid.PreviewMouseLeftButtonUp += preMLBup_removeSliderHandler;
            RootGrid.LostMouseCapture += LostMouseCapture_removeSliderHandler;

            RGBAdock.LostKeyboardFocus += LostKeyFocus_RGBApanel;
            RGBAdock.KeyDown += KeyDown_RGBApanel;

            rectInitialColor.MouseLeftButtonDown += RevertIniColor;
            if (ShowAlpha)
            {
                dockAlpha.Visibility= Visibility.Visible;
            }
            else
            {
                dockAlpha.Visibility= Visibility.Collapsed;
            }
        }

        // --------- Dependency Properties ---------
        public static readonly DependencyProperty TextBoxBackgroundProperty = DependencyProperty.Register("TextBoxBackground", typeof(Brush), typeof(ColorControlPanel),
                                                            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                                                                                          FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush TextBoxBackground
        {
            get { return (Brush)GetValue(TextBoxBackgroundProperty); }
            set { SetValue(TextBoxBackgroundProperty, value); }
        } // Brush TextBoxBackground ////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        public static readonly DependencyProperty TextBoxBorderProperty = DependencyProperty.Register("TextBoxBorder", typeof(Brush), typeof(ColorControlPanel),
                                                            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                                                                                          FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush TextBoxBorder
        {
            get { return (Brush)GetValue(TextBoxBorderProperty); }
            set { SetValue(TextBoxBorderProperty, value); }
        } // Brush TextBoxBorder ////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        public static readonly DependencyProperty TextForegroundProperty = DependencyProperty.Register("TextForeground", typeof(Brush), typeof(ColorControlPanel),
                                                            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(185, 185, 185)),
                                                                                          FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush TextForeground
        {
            get { return (Brush)GetValue(TextForegroundProperty); }
            set { SetValue(TextForegroundProperty, value); }
        } // Brush TextForeground ////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public static readonly DependencyProperty DockAlphaVisibilityProperty = DependencyProperty.Register("DockAlphaVisibility", typeof(Visibility), typeof(ColorControlPanel),
                                                            new FrameworkPropertyMetadata(Visibility.Visible, FrameworkPropertyMetadataOptions.AffectsRender));

        public Visibility DockAlphaVisibility
        {
            get { return (Visibility)GetValue(DockAlphaVisibilityProperty); }
            set { SetValue(DockAlphaVisibilityProperty, value); }
        } // DockAlphaVisibility ////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public static readonly DependencyProperty InitialColorBrushProperty = DependencyProperty.Register("InitialColorBrush", typeof(SolidColorBrush), typeof(ColorControlPanel),
                                                            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                                                                                          new PropertyChangedCallback(IniColorChanged)));

        public SolidColorBrush InitialColorBrush
        {
            get { return (SolidColorBrush)GetValue(InitialColorBrushProperty); }
            set { SetValue(InitialColorBrushProperty, value); }
        }

        private static void IniColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorControlPanel ccp = (ColorControlPanel)d;

            ccp.iniColor = (e.NewValue as SolidColorBrush).Color;
            ccp.iniColorBrush.Color = ccp.iniColor;
        }

        // SolidColorBrush InitialColor ////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        public static readonly DependencyProperty SelectedColorBrushProperty = DependencyProperty.Register("SelectedColorBrush", typeof(SolidColorBrush), typeof(ColorControlPanel),
                                                            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                                                                                          new PropertyChangedCallback(SelectedColorChanged)));

        public SolidColorBrush SelectedColorBrush
        {
            get { return (SolidColorBrush)GetValue(SelectedColorBrushProperty); }
            set { SetValue(SelectedColorBrushProperty, value); }
        }

        private static void SelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorControlPanel ccp = (ColorControlPanel)d;

            if (ccp.outColorBrush != e.NewValue as SolidColorBrush)
            {
                ccp.outColorBrush = e.NewValue as SolidColorBrush;
                ccp.rectSelectedColor.Background = ccp.outColorBrush;
            }

            if (ccp.ThumbsInitialised)
            {
                if (ccp.useHSV)
                {
                    ccp.useHSV = false;
                    ccp.AdjustThumbs(ccp.outColorH, ccp.outColorS, ccp.outColorV);
                }
                else ccp.AdjustThumbs(ccp.outColorBrush.Color);
            }
        }
        // SolidColorBrush InitialColor ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            Slider slider = sender as Slider;
            if (slider != null)
            {
                slider.CaptureMouse();
            }
        }

        private void Slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            Slider slider = sender as Slider;
            if (slider != null)
            {
                slider.ReleaseMouseCapture();
            }
        }

        private void Slider_MouseMove(object sender, MouseEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null && isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(slider);
                double newValue = slider.Minimum + ((slider.Maximum - slider.Minimum) * currentPosition.X / slider.ActualWidth);
                slider.Value = newValue;
            }
        }

        private void Slider_MouseMoveHue(object sender, MouseEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null && isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(slider);
                double newValue = slider.Minimum + ((slider.Maximum - slider.Minimum) * (slider.ActualHeight - currentPosition.Y) / slider.ActualHeight);
                slider.Value = newValue;
            }
        }

    }
}
