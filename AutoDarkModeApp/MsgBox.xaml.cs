using System;
using System.Diagnostics;
using System.Windows;

namespace AutoDarkModeApp
{
    /// <summary>
    /// Interaction logic for MsgBox.xaml
    /// </summary>
    public partial class MsgBox
    {
        public MsgBox(string pMessageText, string pTitle, string pIcon, string pButton)
        {
            InitializeComponent();
            Text_Textblock.Text = pMessageText;
            Title = pTitle;
            PickIcon(pIcon);
            ButtonLayout(pButton);
        }

        private void PickIcon(string pIcon)
        {
            if (pIcon.Equals("error"))
            {
                IconTextBlock.Text = "\xEA39";
            }
            else if (pIcon.Equals("info"))
            {
                IconTextBlock.Text = "\xE946";
            }
            else if (pIcon.Equals("update"))
            {
                IconTextBlock.Text = "\xECC5";
            }
            else if (pIcon.Equals("smiley"))
            {
                IconTextBlock.Text = "\xED54";
            }
        }

        private void ButtonLayout(string pButton)
        {
            if (pButton.Equals("close"))
            {
                CloseButton.Content = Properties.Resources.msgClose;
                YesButton.Visibility = Visibility.Hidden;
            }
            else if (pButton.Equals("yesno"))
            {
                CloseButton.Content = Properties.Resources.msgNo;
                YesButton.Content = Properties.Resources.MsgYes;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public static void ShowErrorMessage(DependencyObject window, Exception ex)
        {
            string error = Properties.Resources.errorThemeApply + "\n\nError ocurred in: " + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = GetWindow(window)
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
        }
    }
}
