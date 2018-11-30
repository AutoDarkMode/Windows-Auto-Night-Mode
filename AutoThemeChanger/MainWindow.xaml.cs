using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TaskShedHandler taskShedHandler = new TaskShedHandler();
        RegEditHandler RegEditHandler = new RegEditHandler();
        Updater updater = new Updater();

        public MainWindow()
        {
            InitializeComponent();
            updater.CheckNewVersion();

            //check if task already exists
            if (taskShedHandler.CheckExistingClass() != null)
            {
                autoRadio.IsChecked = true;
                darkStartBox.Text = Convert.ToString(taskShedHandler.GetRunTime("dark"));
                lightStartBox.Text = Convert.ToString(taskShedHandler.GetRunTime("light"));
                UiHandler();
            }
            else
            {
                //check which value the registry key has
                if (RegEditHandler.AppsUseLightTheme() == true) lightRadio.IsChecked = true;
                else if (RegEditHandler.AppsUseLightTheme() == false) darkRadio.IsChecked = true;
                UiHandler();
            }
        }

        private void LightRadio_Click(object sender, RoutedEventArgs e)
        {
            RegEditHandler.ThemeToLight();
            taskShedHandler.RemoveTask();
            UiHandler();
        }

        private void DarkRadio_Click(object sender, RoutedEventArgs e)
        {
            RegEditHandler.ThemeToDark();
            taskShedHandler.RemoveTask();
            UiHandler();
        }

        private void UiHandler()
        {
            if(autoRadio.IsChecked.Value == true)
            {
                applyButton.IsEnabled = true;
                darkStartBox.IsEnabled = true;
                lightStartBox.IsEnabled = true;
                userFeedback.Text = "click on apply to save changes";
            }
            else if(autoRadio.IsChecked.Value == false)
            {
                applyButton.IsEnabled = false;
                darkStartBox.IsEnabled = false;
                lightStartBox.IsEnabled = false;
                userFeedback.Text = "Choose change automatic to enable auto switch";
            }
        }

        private void AutoRadio_Click(object sender, RoutedEventArgs e)
        {
            UiHandler();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            //get values from TextBox
            int darkStart = int.Parse(darkStartBox.Text);
            int lightStart = int.Parse(lightStartBox.Text);

            //check values from TextBox
            if(darkStart > 24)
            {
                darkStart = 24;
                darkStartBox.Text = Convert.ToString(darkStart);
            }
            if(lightStart >= darkStart)
            {
                lightStart = darkStart - 1;
                lightStartBox.Text = Convert.ToString(lightStart);
            }
            if (lightStart < 0)
            {
                lightStart = 23;
                lightStartBox.Text = Convert.ToString(lightStart);
            }

            try
            {
                taskShedHandler.CreateTask(darkStart, lightStart);

                //fit current theme to the time
                var time = DateTime.Now.Hour;
                if (time <= lightStart || time >= darkStart) RegEditHandler.ThemeToDark();
                else if (time >= lightStart || time <= darkStart) RegEditHandler.ThemeToLight();

                //UI
                userFeedback.Text = "changes were saved!";
            }
            catch{
                userFeedback.Text = "error occurred :(";
            }
        }

        //textbox allow only numbers
        private void LightStartBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void DarkStartBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        //textbox block cut, copy & paste
        private void LightStartBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void DarkStartBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        //open aboutWindow
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}