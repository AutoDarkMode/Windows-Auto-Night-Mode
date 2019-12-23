using System;
using System.Reflection;
using System.Xml;
using System.Windows;
using System.Globalization;

namespace AutoDarkModeApp
{
    class Updater
    {
        Version newVersion = null;
        Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        string url;
        bool silent = false;
        bool updateAvailable = false;

        public bool SilentUpdater()
        {
            silent = true;
            CheckNewVersion();
            return updateAvailable;
        }

        public string GetURL()
        {
            return url;
        }

        public void CheckNewVersion()
        {
            XmlTextReader reader;
            try
            {
                string xmlURL = "https://raw.githubusercontent.com/Armin2208/Windows-Auto-Night-Mode/master/version.xml";
                reader = new XmlTextReader(xmlURL);
                reader.MoveToContent();
                string elementName = "AutoNightMode";
                if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "AutoNightMode"))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element) elementName = reader.Name;
                        else
                        {
                            if ((reader.NodeType == XmlNodeType.Text) && (reader.HasValue))
                            {
                                switch (elementName)
                                {
                                    case "version":
                                        newVersion = new Version(reader.Value);
                                        break;
                                    case "url":
                                        url = reader.Value;
                                        break;
                                }
                            }
                        }
                    }
                }
                reader.Close();
                MessageBoxHandler();
            }
            catch
            {

            }
        }

        private void MessageBoxHandler()
        {
            CultureInfo.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Language, true);
            if (currentVersion.CompareTo(newVersion) < 0)
            {
                if (!silent)
                {
                    string text = String.Format(Properties.Resources.msgUpdaterText, currentVersion, newVersion);
                    MsgBox msgBox = new MsgBox(text, "Auto Dark Mode Updater", "update", "yesno")
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen, Topmost = true
                    };
                    msgBox.ShowDialog();
                    var result = msgBox.DialogResult;
                    if (result == true)
                    {
                        System.Diagnostics.Process.Start(url);
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    updateAvailable = true;
                }
            }
        }
    }
}