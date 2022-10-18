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