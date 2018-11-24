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
        taskShedHandler taskShedHandler = new taskShedHandler();
        RegEditHandler RegEditHandler = new RegEditHandler();
        Updater updater = new Updater();

        public MainWindow()
        {
            InitializeComponent();
            updater.checkNewVersion();

            //check if task already exists
            if (taskShedHandler.checkExistingClass() != null)
            {
                autoRadio.IsChecked = true;
                darkStartBox.Text = Convert.ToString(taskShedHandler.getRunTime("dark"));
                lightStartBox.Text = Convert.ToString(taskShedHandler.getRunTime("light"));
                uiHandler();
            }
            else
            {
                //check which value the registry key has
                if (RegEditHandler.AppsUseLightTheme() == true) lightRadio.IsChecked = true;
                else if (RegEditHandler.AppsUseLightTheme() == false) darkRadio.IsChecked = true;
                uiHandler();
            }
        }

        private void lightRadio_Click(object sender, RoutedEventArgs e)
        {
            RegEditHandler.themeToLight();
            taskShedHandler.removeTask();
            uiHandler();
        }

        private void darkRadio_Click(object sender, RoutedEventArgs e)
        {
            RegEditHandler.themeToDark();
            taskShedHandler.removeTask();
            uiHandler();
        }

        private void uiHandler()
        {
            if(autoRadio.IsChecked.Value == true)
            {
                applyButton.IsEnabled = true;
                darkStartBox.IsEnabled = true;
                lightStartBox.IsEnabled = true;
                userFeedback.Text = "click on apply to save changes\n\nbefore uninstalling the program,\nplease switch to light or dark";
            }
            else if(autoRadio.IsChecked.Value == false)
            {
                applyButton.IsEnabled = false;
                darkStartBox.IsEnabled = false;
                lightStartBox.IsEnabled = false;
                userFeedback.Text = "Choose automatic to enable auto switch";
            }
        }

        private void autoRadio_Click(object sender, RoutedEventArgs e)
        {
            uiHandler();
        }

        private void applyButton_Click(object sender, RoutedEventArgs e)
        {
            //get Values from TextBox
            int darkStart = int.Parse(darkStartBox.Text);
            int lightStart = int.Parse(lightStartBox.Text);

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
                taskShedHandler.createTask(darkStart, lightStart);

                //fit current theme to the time
                var time = DateTime.Now.Hour;
                if (time <= lightStart || time >= darkStart) RegEditHandler.themeToDark();
                else if (time >= lightStart || time <= darkStart) RegEditHandler.themeToLight();

                //UI
                userFeedback.Text = "changes were saved!\n\nbefore uninstalling the program,\nplease switch to light or dark";
            }
            catch{
                userFeedback.Text = "error occurred :(";
            }
        }

        //textbox numbers only allowed
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

        //textbox block cut copy paste
        private void lightStartBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void darkStartBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            aboutWindow aboutWindow = new aboutWindow();
            aboutWindow.ShowDialog();
            
        }
    }
}