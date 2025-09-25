using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoDarkModeApp.ViewModels;

public partial class CursorsViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private bool _isInitializing;

    [ObservableProperty]
    public partial bool CursorsEnabled { get; set; }

    [ObservableProperty]
    public partial object? SelectLightCursor { get; set; }

    [ObservableProperty]
    public partial object? SelectDarkCursor { get; set; }

    public CursorsViewModel(IErrorService errorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

        try
        {
            _builder.Load();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationPage");
        }

        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        CursorsEnabled = _builder.Config.CursorSwitch.Enabled;
        SelectLightCursor = _builder.Config.CursorSwitch.Component.CursorsLight;
        SelectDarkCursor = _builder.Config.CursorSwitch.Component.CursorsDark;

        _isInitializing = false;
    }

    private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            StateUpdateHandler.StopConfigWatcher();

            _builder.Load();
            LoadSettings();

            StateUpdateHandler.StartConfigWatcher();
        });
    }

    partial void OnCursorsEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.CursorSwitch.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CursorsPage");
        }
        RequestThemeSwitch();
    }

    partial void OnSelectLightCursorChanged(object? value)
    {
        if (_isInitializing)
            return;

        if (value != null)
        {
            _builder.Config.CursorSwitch.Component.CursorsLight = value.ToString();
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CursorsPage");
        }
        RequestThemeSwitch();
    }

    partial void OnSelectDarkCursorChanged(object? value)
    {
        if (_isInitializing)
            return;

        if (value != null)
        {
            _builder.Config.CursorSwitch.Component.CursorsDark = value.ToString();
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CursorsPage");
        }
        RequestThemeSwitch();
    }



    private async void RequestThemeSwitch()
    {
        try
        {
            var result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException(result, "CursorsViewModel");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CursorsViewModel");
        }
    }
}