using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;

namespace AutoDarkModeSvc.Handler
{
    static class UpdateHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static string CheckNewVersion()
        {
            XmlTextReader reader;
            Version newVersion = null;
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            string url = null;
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
                if (newVersion != null && url != null && currentVersion.CompareTo(newVersion) < 0)
                {
                    Logger.Info($"new version ({newVersion.ToString()} available");
                    return $"{newVersion},{url}";
                }
            }
            catch (Exception e)
            {
                Logger.Warn(e, "update check failed");
            }
            return "";
        }
    }
}
