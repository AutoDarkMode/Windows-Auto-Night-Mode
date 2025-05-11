using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoDarkModeApp.ViewModels;

public partial class ScriptsViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private bool _isInitializing;

    [ObservableProperty]
    public partial bool ScriptSwitchEnabled { get; set; }

    public ScriptsViewModel(IErrorService errorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

        try
        {
            _builder.LoadScriptConfig();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ScriptsViewModel");
        }

        LoadSettings();

        StateUpdateHandler.OnScriptConfigUpdate += HandleScriptConfigUpdate;
        StateUpdateHandler.StartScriptWatcher();
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        ScriptSwitchEnabled = _builder.ScriptConfig.Enabled;

        _isInitializing = false;
    }

    private void HandleScriptConfigUpdate(object sender, FileSystemEventArgs e)
    {
        StateUpdateHandler.StopScriptWatcher();
        _dispatcherQueue.TryEnqueue(() =>
        {
            _builder.LoadScriptConfig();
            LoadSettings();
        });
        StateUpdateHandler.StartScriptWatcher();
    }

    partial void OnScriptSwitchEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.ScriptConfig.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ScriptsViewModel");
        }
    }
}
