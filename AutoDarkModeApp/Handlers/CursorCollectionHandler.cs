using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeApp.Handlers
{
    public static class CursorCollectionHandler
    {
        public static List<string> GetCursors()
        {
            using RegistryKey cursorsKeyUser = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors\Schemes");
            using RegistryKey cursorsKeySystem = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Cursors\Schemes");
            List<string> cursors = cursorsKeyUser.GetValueNames().ToArray().ToList();
            cursors.AddRange(cursorsKeySystem.GetValueNames());
            return cursors;
        }

        public static string GetCurrentCursorScheme()
        {
            using RegistryKey cursorsKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors");
            return (string)cursorsKey.GetValue("");
        }

        public static string[] GetCursorScheme(string name)
        {

            using RegistryKey cursorsKeyUser = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors\Schemes");
            using RegistryKey cursorsKeySystem = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Cursors\Schemes");
            List<string> cursorsUser = cursorsKeyUser.GetValueNames().ToArray().ToList();
            List<string> cursorsSystem = cursorsKeySystem.GetValueNames().ToArray().ToList();

            string userTheme = cursorsUser.Where(x => x == name).FirstOrDefault();
            string systemTheme = cursorsSystem.Where(x => x == name).FirstOrDefault();
            string[] cursorsList = { };

            if (userTheme != null)
            {
                cursorsList = ((string)cursorsKeyUser.GetValue(userTheme)).Split(",");
            }
            else if (systemTheme != null)
            {
                cursorsList = ((string)cursorsKeySystem.GetValue(systemTheme)).Split(",");
            }

            return cursorsList;
        }
    }


}
