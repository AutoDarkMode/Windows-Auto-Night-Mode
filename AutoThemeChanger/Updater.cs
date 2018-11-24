using System;
using System.Reflection;
using System.Xml;
using System.Windows;

namespace AutoThemeChanger
{
    class Updater
    {
        Version newVersion = null;
        Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        string url;
        bool silent = false;
        bool updateAvailable = false;

        public bool silentUpdater()
        {
            silent = true;
            checkNewVersion();
            return updateAvailable;
        }

        public string getURL()
        {
            return url;
        }

        public void checkNewVersion()
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
                messageBoxHandler();
            }
            catch
            {

            }
        }

        private void messageBoxHandler()
        {
            if (currentVersion.CompareTo(newVersion) < 0)
            {
                if (!silent)
                {
                    string text = "Thank you for using Auto-Night Mode!\n\nA new version is available on GitHub with fixes and enhancements.\nDo you want to download it?\n\nCurrently installed version: " + currentVersion + ", new Version: " + newVersion;
                    var result = MessageBox.Show(text, "Auto-Night Mode Updater", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(url);
                        Application.Current.Shutdown();
                    }
                }else
                {
                    updateAvailable = true;
                }
            }
        }
    }
}