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
using System.Windows;
using AdmProperties = AutoDarkModeLib.Properties;

namespace AutoDarkModeApp
{
    /// <summary>
    /// Interaction logic for MsgBox.xaml
    /// Parameters:
    /// Icons: error, info, update, smiley
    /// Buttons: close, yesno
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
                CloseButton.Content = AdmProperties.Resources.msgClose;
                YesButton.Visibility = Visibility.Hidden;
            }
            else if (pButton.Equals("yesno"))
            {
                CloseButton.Content = AdmProperties.Resources.msgNo;
                YesButton.Content = AdmProperties.Resources.MsgYes;
            }
            else if (pButton.Equals("none"))
            {
                CloseButton.Visibility = Visibility.Hidden;
                YesButton.Visibility = Visibility.Hidden;
            }
            else if (pButton.Equals("okcancel"))
            {
                CloseButton.Content = AdmProperties.Resources.cancel;
                YesButton.Content = AdmProperties.Resources.ok;
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
    }
}
