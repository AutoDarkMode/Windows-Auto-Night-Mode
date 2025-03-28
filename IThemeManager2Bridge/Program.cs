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
using AutoDarkModeComms;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;

namespace IThemeManager2Bridge
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(BridgeResponseCode.InvalidArguments);
            }

            string displayName = args[0];

            bool tm2Found = false;
            bool tm2Success = false;

            IMessageClient client = new PipeClient();
            ApiResponse response = ApiResponse.FromString(client.SendMessageAndGetReply(Command.GetLearnedThemeNames));

            if (response.StatusCode == StatusCode.Ok)
            {
                try
                {
                    ThemeDllWrapper.LearnedThemeNames = Helper.DeserializeLearnedThemesDict(response.Message);
                }
                // not sure how to pass data back to service best for this, as it doesn't impact functionality if deser fails here.
                catch { }
            }

            try
            {
                (tm2Found, tm2Success) = ThemeDllWrapper.SetTheme(displayName);
            }
            catch (Exception ex)
            {
                if (ex.Data.Contains("UserMessage"))
                {
                    Console.WriteLine($"{BridgeResponseCode.Fail}{ApiResponse.separator}{ex.Message} {ex.Data["UserMessage"]}");
                }
            }

            if (tm2Success) Console.WriteLine(BridgeResponseCode.Success);

            if (!tm2Found) Console.WriteLine(BridgeResponseCode.NotFound);

            Environment.Exit(0);
        }
    }
}