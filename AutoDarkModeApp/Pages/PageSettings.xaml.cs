using AutoDarkModeApp.Properties;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageSettings.xaml
    /// </summary>
    public partial class PageSettings : Page
    {
        readonly string curLanguage = Settings.Default.Language;

        public PageSettings()
        {
            InitializeComponent();
            UiHandler();
        }
        private void UiHandler()
        {
            RestartButton.Visibility = Visibility.Hidden;
            ComboBoxLanguageSelection.SelectedValue = Settings.Default.Language.ToString();
            if(ComboBoxLanguageSelection.SelectedValue == null)
            {
                ComboBoxLanguageSelection.SelectedValue = "en";
            }

            if (!Settings.Default.Enabled)
            {
                CheckBoxConStandBy.IsEnabled = false;
                CheckBoxLogonTask.IsEnabled = false;
                CheckBoxColourFilter.IsEnabled = false;
                CheckBoxMultiUserImprovements.IsEnabled = false;
                CheckBoxBackgroundUpdater.IsEnabled = false;
            }

            CheckBoxAlterTime.IsChecked = Settings.Default.AlterTime;
            CheckBoxBackgroundUpdater.IsChecked = Settings.Default.BackgroundUpdate;
            CheckBoxConStandBy.IsChecked = Settings.Default.connectedStandby;
            CheckBoxLogonTask.IsChecked = Settings.Default.LogonTaskInsteadOfAutostart;
            CheckBoxColourFilter.IsChecked = Settings.Default.ColourFilterKeystroke;
            CheckBoxMultiUserImprovements.IsChecked = Settings.Default.TaskFolderTitleMultiUser;

            TextboxAccentColorDelay.Text = Settings.Default.AccentColorSwitchTime.ToString();
        }

        private void ComboBoxLanguageSelection_DropDownClosed(object sender, System.EventArgs e)
        {
            string selectedLanguage = ComboBoxLanguageSelection.SelectedValue.ToString();
            if (selectedLanguage != curLanguage)
            {
                SetLanguage(selectedLanguage);
                Translator.Text = Properties.Resources.lblTranslator;
                RestartText.Text = Properties.Resources.restartNeeded;
                RestartButton.Content = Properties.Resources.restart;
                RestartButton.Visibility = Visibility.Visible;

            }
            else
            {
                SetLanguage(selectedLanguage);
                RestartText.Text = null;
                RestartButton.Visibility = Visibility.Hidden;
                Translator.Text = Properties.Resources.lblTranslator;
            }
        }

        private void SetLanguage(string lang)
        {
            Settings.Default.Language = lang;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(Settings.Default.Language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.Language);
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Settings.Default.Save();
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
            catch
            {

            }
        }

        private void CheckBoxAlterTime_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxAlterTime.IsChecked.Value)
            {
                Settings.Default.AlterTime = true;
            }
            else
            {
                Settings.Default.AlterTime = false;
            }
        }

        private void CheckBoxBackgroundUpdater_Click(object sender, RoutedEventArgs e)
        {
            TaskSchHandler taskShedHandler = new TaskSchHandler();

            if (CheckBoxBackgroundUpdater.IsChecked.Value)
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

        private void CheckBoxConStandBy_Click(object sender, RoutedEventArgs e)
        {
            TaskSchHandler taskShedHandler = new TaskSchHandler();

            if (CheckBoxConStandBy.IsChecked.Value)
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

        private void CheckBoxLogonTask_Click(object sender, RoutedEventArgs e)
        {
            RegeditHandler regeditHandler = new RegeditHandler();
            TaskSchHandler taskScheduler = new TaskSchHandler();

            if (CheckBoxLogonTask.IsChecked.Value)
            {
                regeditHandler.RemoveAutoStart();
                taskScheduler.CreateLogonTask();
                Settings.Default.LogonTaskInsteadOfAutostart = true;
            }
            else
            {
                taskScheduler.RemoveLogonTask();
                regeditHandler.AddAutoStart();
                Settings.Default.LogonTaskInsteadOfAutostart = false;
            }
        }

        private void CheckBoxColourFilter_Click(object sender, RoutedEventArgs e)
        {
            RegeditHandler regeditHandler = new RegeditHandler();

            if(CheckBoxColourFilter.IsChecked.Value)
            {
                regeditHandler.ColourFilterSetup();
                Settings.Default.ColourFilterKeystroke = true;
            }
            else
            {
                regeditHandler.ColourFilterKeySender(false);
                Settings.Default.ColourFilterKeystroke = false;
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
                Settings.Default.AccentColorSwitchTime = int.Parse(TextboxAccentColorDelay.Text);
            }
        }

        private void CheckBoxMultiUserImprovements_Click(object sender, RoutedEventArgs e)
        {
            TaskSchHandler taskScheduler = new TaskSchHandler();
            RegeditHandler regEditHandler = new RegeditHandler();

            if (CheckBoxMultiUserImprovements.IsChecked == true)
            {
                taskScheduler.RemoveAllTasks();
                Settings.Default.TaskFolderTitle = "ADM_" + Environment.UserName;
                Settings.Default.TaskFolderTitleMultiUser = true;
            }
            else
            {
                taskScheduler.RemoveAllTasks();
                Settings.Default.TaskFolderTitle = "Auto Dark Mode";
                Settings.Default.TaskFolderTitleMultiUser = false;
            }
            if (!Settings.Default.LogonTaskInsteadOfAutostart)
            {
                regEditHandler.RemoveAutoStart();
            }
            Settings.Default.Enabled = false;
            RestartButton_Click(this, null);
        }
    }
}
