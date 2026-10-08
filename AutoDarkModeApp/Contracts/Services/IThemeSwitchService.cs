namespace AutoDarkModeApp.Contracts.Services;

public interface IThemeSwitchService
{
    Task RequestThemeSwitchAsync(string source, XamlRoot xamlRoot);
}
