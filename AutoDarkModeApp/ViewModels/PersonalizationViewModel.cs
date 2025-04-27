using System.Windows.Input;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.ViewModels;

public partial class PersonalizationViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;

    [ObservableProperty]
    public partial bool ThemeSwitchEnabled { get; set; }

    [ObservableProperty]
    public partial bool ManagedModeSettingsCardEnabled { get; set; }

    // TODO: Replace by better name
    [ObservableProperty]
    public partial bool ThemeSwitchDisabled { get; set; }

    [ObservableProperty]
    public partial bool UnmanagedModeSettingsCardEnabled { get; set; }

    public ICommand ThemeSwitchDisabledCommand { get; }

    public PersonalizationViewModel(IErrorService errorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

        try
        {
            _builder.Load();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "PersonalizationPage");
        }

        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();

        ThemeSwitchDisabledCommand = new RelayCommand(() =>
        {
            _builder.Config.WindowsThemeMode.Enabled = false;
            try
            {
                _builder.Save();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "PersonalizationPage");
            }
        });
    }

    private void LoadSettings()
    {
        ThemeSwitchEnabled = _builder.Config.WindowsThemeMode.Enabled;
        ManagedModeSettingsCardEnabled = !_builder.Config.WindowsThemeMode.Enabled;
        ThemeSwitchDisabled = _builder.Config.WallpaperSwitch.Enabled || _builder.Config.ColorizationSwitch.Enabled || _builder.Config.CursorSwitch.Enabled;
        UnmanagedModeSettingsCardEnabled = !(_builder.Config.WallpaperSwitch.Enabled || _builder.Config.ColorizationSwitch.Enabled || _builder.Config.CursorSwitch.Enabled);
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

    internal void OnViewModelNavigatedFrom(NavigationEventArgs e)
    {
        StateUpdateHandler.OnConfigUpdate -= HandleConfigUpdate;
        StateUpdateHandler.StopConfigWatcher();
    }
}
