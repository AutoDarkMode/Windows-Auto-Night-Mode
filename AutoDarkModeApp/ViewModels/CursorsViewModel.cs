using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoDarkModeApp.ViewModels;

public partial class CursorsViewModel : ObservableRecipient
{
    private const string Location = "CursorsViewModel";
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
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

            RequestDebouncedSave();
            StateUpdateHandler.StartConfigWatcher();
        });
    }

    partial void OnCursorsEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.CursorSwitch.Enabled = value;
        RequestDebouncedSave();

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }
    }

    partial void OnSelectLightCursorChanged(object? value)
    {
        if (_isInitializing)
            return;

        if (value != null)
        {
            _builder.Config.CursorSwitch.Component.CursorsLight = value.ToString();
        }
        RequestDebouncedSave();

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }
    }

    partial void OnSelectDarkCursorChanged(object? value)
    {
        if (_isInitializing)
            return;

        if (value != null)
        {
            _builder.Config.CursorSwitch.Component.CursorsDark = value.ToString();
        }
        RequestDebouncedSave();

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }
    }

    private async void RequestDebouncedSave()
    {
        // Logic to handle debounced saving
        // This could involve setting a timer or flag to delay the save operation
        await Task.Delay(500); // Example delay for debouncing
        try
        {
            // Call the save method on the builder
            _builder.Save();
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur during saving
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }
    }
}