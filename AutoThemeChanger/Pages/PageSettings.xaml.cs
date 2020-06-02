using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoThemeChanger.Pages
{
    /// <summary>
    /// Interaction logic for PageSettings.xaml
    /// </summary>
    public partial class PageSettings : Page
    {
        readonly string curLanguage = Properties.Settings.Default.Language;

        public PageSettings()
        {
            InitializeComponent();
            UiHandler();
        }
        private void UiHandler()
        {
            if (!Properties.Settings.Default.Enabled)
            {
                conStandByCB.IsEnabled = false;
            }

            LangComBox.SelectedValue = Properties.Settings.Default.Language.ToString();

            if (Properties.Settings.Default.AlterTime)
            {
                AlterTimeCheckBox.IsChecked = true;
            }
            if (Properties.Settings.Default.BackgroundUpdate)
            {
                BckgrUpdateCB.IsChecked = true;
            }
            if (Properties.Settings.Default.connectedStandby)
            {
                conStandByCB.IsChecked = true;
            }

            TextboxAccentColorDelay.Text = Properties.Settings.Default.AccentColorSwitchTime.ToString();
        }

        private void ComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            SetLanguage(LangComBox.SelectedValue.ToString());
            Translator.Text = Properties.Resources.lblTranslator;
            if (Properties.Settings.Default.Language != curLanguage)
            {
                RestartText.Text = Properties.Resources.restartNeeded;
            }
            else
            {
                RestartText.Text = null;
            }
        }

        private void SetLanguage(string lang)
        {
            Properties.Settings.Default.Language = lang;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(Properties.Settings.Default.Language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Language);
        }

        private void AboutWindowXAML_Closed(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.Language != curLanguage)
            {
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
            else
            {
                //Close(); TODO MUSS SPEICHERN BUTTON HIN
            }
        }

        private void AlterTimeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (AlterTimeCheckBox.IsChecked.Value)
            {
                Properties.Settings.Default.AlterTime = true;
            }
            else
            {
                Properties.Settings.Default.AlterTime = false;
            }
        }

        private void BckgrUpdateCB_Click(object sender, RoutedEventArgs e)
        {
            TaskSchHandler taskShedHandler = new TaskSchHandler();

            if (BckgrUpdateCB.IsChecked.Value)
            {
                taskShedHandler.CreateAppUpdaterTask();
                Properties.Settings.Default.BackgroundUpdate = true;
            }
            else
            {
                taskShedHandler.RemoveAppUpdaterTask();
                Properties.Settings.Default.BackgroundUpdate = false;
            }
        }

        private void ConStandByCB_Click(object sender, RoutedEventArgs e)
        {
            TaskSchHandler taskShedHandler = new TaskSchHandler();

            if (conStandByCB.IsChecked.Value)
            {
                taskShedHandler.CreateConnectedStandbyTask();
                Properties.Settings.Default.connectedStandby = true;
            }
            else
            {
                taskShedHandler.RemoveConnectedStandbyTask();
                Properties.Settings.Default.connectedStandby = false;
            }
        }

        private void TextboxAccentColorDelay_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextboxAccentColorDelay.Dispatcher.BeginInvoke(new Action(() =>
            {
                TextboxAccentColorDelay.SelectAll();
            }));
        }

        private void TextboxAccentColorDelay_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextboxAccentColorDelay_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void TextboxAccentColorDelay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextboxAccentColorDelay.Text != "")
            {
                Properties.Settings.Default.AccentColorSwitchTime = int.Parse(TextboxAccentColorDelay.Text);
            }
        }
    }
}
