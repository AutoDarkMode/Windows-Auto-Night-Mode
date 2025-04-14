using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.ViewModels;

public partial class CursorsViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;

    [ObservableProperty]
    public partial bool IsCursorsEnabled { get; set; }

    [ObservableProperty]
    public partial object? SelectLightCusor { get; set; }

    [ObservableProperty]
    public partial object? SelectDarkCusor { get; set; }

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
        IsCursorsEnabled = _builder.Config.CursorSwitch.Enabled;
        SelectLightCusor = _builder.Config.CursorSwitch.Component.CursorsLight;
        SelectDarkCusor = _builder.Config.CursorSwitch.Component.CursorsDark;
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

    partial void OnIsCursorsEnabledChanged(bool value)
    {
        _builder.Config.CursorSwitch.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CursorsPage");
        }
    }

    partial void OnSelectLightCusorChanged(object? value)
    {
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
    }

    partial void OnSelectDarkCusorChanged(object? value)
    {
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
    }

    internal void OnViewModelNavigatedFrom(NavigationEventArgs e)
    {
        StateUpdateHandler.OnConfigUpdate -= HandleConfigUpdate;
        StateUpdateHandler.StopConfigWatcher();
    }
}
