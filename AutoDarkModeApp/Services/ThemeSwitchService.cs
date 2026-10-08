namespace AutoDarkModeApp.Services;

public class ThemeSwitchService(IErrorService errorService) : IThemeSwitchService
{
    public async Task RequestThemeSwitchAsync(string source, XamlRoot xamlRoot)
    {
        try
        {
            var result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException(result, source);
            }
        }
        catch (Exception ex)
        {
            await errorService.ShowErrorMessage(ex, xamlRoot, source);
        }
    }
}
