using System.Diagnostics;
using System.Windows.Input;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.System.Power;

namespace AutoDarkModeApp.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly Updater _updater;
    private readonly IErrorService _errorService;
    private readonly ILocalSettingsService _localSettingsService;
    private bool _isInitializing;

    public enum DaysBetweenUpdateCheck
    {
        OneDay,
        ThreeDay,
        OneWeek,
        TwoWeeks,
    }

    public enum UpdateChannel
    {
        Stable,
        Beta,
    }

    [ObservableProperty]
    public partial bool IsHideTray { get; set; }

    [ObservableProperty]
    public partial bool IsAlwaysFullDwmRefresh { get; set; }

    [ObservableProperty]
    public partial bool IsTunableDebug { get; set; }

    [ObservableProperty]
    public partial bool IsTunableTrace { get; set; }

    [ObservableProperty]
    public partial bool EnergySaverSettingsCardVisiblity { get; set; }

    [ObservableProperty]
    public partial bool IsDisableEnergySaverOnThemeSwitch { get; set; }

    [ObservableProperty]
    public partial int BatterySliderValue { get; set; }

    [ObservableProperty]
    public partial bool IsUpdaterEnabled { get; set; }

    [ObservableProperty]
    public partial string? UpdatesDate { get; set; }

    [ObservableProperty]
    public partial string? Language { get; set; }

    [ObservableProperty]
    public partial bool IsLanguageChangedInfoBarOpen { get; set; }

    [ObservableProperty]
    public partial DaysBetweenUpdateCheck SelectedDaysBetweenUpdateCheck { get; set; }

    [ObservableProperty]
    public partial bool IsCheckOnStart { get; set; }

    [ObservableProperty]
    public partial bool IsAutoInstall { get; set; }

    [ObservableProperty]
    public partial bool IsUpdateSilent { get; set; }

    [ObservableProperty]
    public partial UpdateChannel SelectedUpdateChannel { get; set; }

    [ObservableProperty]
    public partial bool IsAutostart { get; set; }

    [ObservableProperty]
    public partial bool IsLoginWithTask { get; set; }

    [ObservableProperty]
    public partial string? AutostartMode { get; set; }

    [ObservableProperty]
    public partial string? AutostartPath { get; set; }

    public ICommand RestartCommand { get; }

    public ICommand CheckUpdateCommand { get; }

    public ICommand AutostartRefreshCommand { get; }

    public SettingsViewModel(IErrorService errorService, ILocalSettingsService localSettingsService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _updater = new();
        _errorService = errorService;
        _localSettingsService = localSettingsService;

        try
        {
            _builder.Load();
            _builder.LoadUpdaterData();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SettingsViewModel");
        }

        LoadSettings();
        _dispatcherQueue.TryEnqueue(async () => await GetAutostartInfo());

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();

        RestartCommand = new RelayCommand(() =>
        {
            _builder.Config.Tunable.UICulture = Localization.LanguageTranscoding(Language!);
            try
            {
                _builder.Save();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SettingsViewModel");
            }

            MessageHandler.Client.SendMessageAndGetReply(Command.Restart);
            Process.Start(new ProcessStartInfo(Helper.ExecutionPathApp) { UseShellExecute = false, Verb = "open" });
            App.Current.Exit();
        });

        CheckUpdateCommand = new RelayCommand(() =>
        {
            UpdatesDate = "Msg_SearchUpdate".GetLocalized();
            Task.Run(() =>
            {
                _updater.CheckNewVersion();
                _dispatcherQueue.TryEnqueue(() =>
                {
                    UpdatesDate = _updater.UpdateAvailable() ? "Msg_UpdateAvailable".GetLocalized() : "Msg_NoUpdate".GetLocalized();
                });
            });
        });

        AutostartRefreshCommand = new RelayCommand(async () =>
        {
            await GetAutostartInfo();
        });
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        IsHideTray = !_builder.Config.Tunable.ShowTrayIcon;
        IsAlwaysFullDwmRefresh = _builder.Config.Tunable.AlwaysFullDwmRefresh;
        IsTunableDebug = _builder.Config.Tunable.Debug;
        IsTunableTrace = _builder.Config.Tunable.Trace;
        EnergySaverSettingsCardVisiblity = !(PowerManager.BatteryStatus == BatteryStatus.NotPresent || Environment.OSVersion.Version.Build >= (int)WindowsBuilds.MinBuildForNewFeatures);
        IsDisableEnergySaverOnThemeSwitch = _builder.Config.Tunable.DisableEnergySaverOnThemeSwitch;
        BatterySliderValue = _builder.Config.Tunable.BatterySliderDefaultValue;
        IsUpdaterEnabled = _builder.Config.Updater.Enabled;
        SelectedDaysBetweenUpdateCheck = _builder.Config.Updater.DaysBetweenUpdateCheck switch
        {
            1 => DaysBetweenUpdateCheck.OneDay,
            3 => DaysBetweenUpdateCheck.ThreeDay,
            7 => DaysBetweenUpdateCheck.OneWeek,
            14 => DaysBetweenUpdateCheck.TwoWeeks,
            _ => DaysBetweenUpdateCheck.OneDay,
        };
        IsCheckOnStart = _builder.Config.Updater.CheckOnStart;
        IsAutoInstall = _builder.Config.Updater.AutoInstall;
        IsUpdateSilent = _builder.Config.Updater.Silent;
        IsLoginWithTask = _builder.Config.Tunable.UseLogonTask;

        if (string.IsNullOrEmpty(_builder.Config.Updater.VersionQueryUrl))
        {
            SelectedUpdateChannel = UpdateChannel.Stable;
        }
        else if (_builder.Config.Updater.VersionQueryUrl.Equals(@"https://raw.githubusercontent.com/AutoDarkMode/AutoDarkModeVersion/master/version-beta.yaml"))
        {
            SelectedUpdateChannel = UpdateChannel.Beta;
        }
        else
        {
            SelectedUpdateChannel = UpdateChannel.Stable;
        }

        if (_builder.UpdaterData.LastCheck.Year.ToString().Equals("1"))
        {
            UpdatesDate = "LastCheckedTime".GetLocalized() + " " + "LastCheckedNever".GetLocalized();
        }
        else
        {
            UpdatesDate = "LastCheckedTime".GetLocalized() + " " + _builder.UpdaterData.LastCheck;
        }

        _dispatcherQueue.TryEnqueue(async () =>
        {
            _isInitializing = true;

            var languageText = await _localSettingsService.ReadSettingAsync<string>("Language");
            if (languageText != null)
            {
                Language = languageText.Replace("\"", "");
            }
            else
            {
                Language = "English (English)";
                await _localSettingsService.SaveSettingAsync("Language", "English (English)");
            }

            _isInitializing = false;
        });

        _isInitializing = false;
    }

    private async Task GetAutostartInfo()
    {
        try
        {
            var autostartResponse = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.GetAutostartState));
            if (autostartResponse.StatusCode == StatusCode.Err)
            {
                await _errorService.ShowErrorMessageFromApi(autostartResponse, new AbandonedMutexException(), App.MainWindow.Content.XamlRoot);
            }
            else if (autostartResponse.StatusCode == StatusCode.AutostartRegistryEntry)
            {
                if (autostartResponse.Message == "Enabled")
                {
                    IsAutostart = true;
                    AutostartMode = "RegistryKey".GetLocalized();
                    AutostartPath = autostartResponse.Details;
                }
                else
                {
                    IsAutostart = false;
                }
            }
            else if (autostartResponse.StatusCode == StatusCode.AutostartTask)
            {
                IsAutostart = true;
                AutostartMode = "Task".GetLocalized();
                AutostartPath = autostartResponse.Details;
            }
            else if (autostartResponse.StatusCode == StatusCode.Disabled)
            {
                IsAutostart = false;
                AutostartMode = "Disabled".GetLocalized();
                AutostartPath = "None".GetLocalized();
            }
        }
        catch (Exception)
        {
            AutostartMode = "Disabled".GetLocalized();
            AutostartPath = "None".GetLocalized();
        }
    }

    private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
    {
        StateUpdateHandler.StopConfigWatcher();
        _builder.Load();
        _builder.LoadUpdaterData();
        _dispatcherQueue.TryEnqueue(() =>
        {
            LoadSettings();
        });
        StateUpdateHandler.StartConfigWatcher();
    }

    partial void OnIsHideTrayChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Tunable.ShowTrayIcon = !value;

        SafeSaveBuilder();
        Task.Run(() => MessageHandler.Client.SendMessageAndGetReply(Command.Restart));
    }

    partial void OnIsAlwaysFullDwmRefreshChanged(bool value)
    {
        if (_isInitializing)
            return;

        if (value)
        {
            ContentDialog contentDialog = new()
            {
                Title = "AlwaysRefreshDwm".GetLocalized(),
                Content = "AlwaysRefreshDwm_Content".GetLocalized(),
                XamlRoot = App.MainWindow.Content.XamlRoot,
                CloseButtonText = "Cancel".GetLocalized(),
                PrimaryButtonText = "Confirm".GetLocalized(),
            };
            _dispatcherQueue.TryEnqueue(async () =>
            {
                var result = await contentDialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                    IsAlwaysFullDwmRefresh = false;
            });
            _builder.Config.Tunable.AlwaysFullDwmRefresh = IsAlwaysFullDwmRefresh;
        }
        else
        {
            _builder.Config.Tunable.AlwaysFullDwmRefresh = IsAlwaysFullDwmRefresh;
        }

        SafeSaveBuilder();
    }

    partial void OnIsTunableDebugChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Tunable.Debug = value;

        SafeSaveBuilder();
    }

    partial void OnIsTunableTraceChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Tunable.Trace = value;

        SafeSaveBuilder();
    }

    partial void OnIsDisableEnergySaverOnThemeSwitchChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Tunable.DisableEnergySaverOnThemeSwitch = value;

        SafeSaveBuilder();
    }

    partial void OnBatterySliderValueChanged(int value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Tunable.BatterySliderDefaultValue = value;

        SafeSaveBuilder();
    }

    partial void OnLanguageChanged(string? value)
    {
        if (_isInitializing)
            return;

        _dispatcherQueue.TryEnqueue(async () =>
        {
            var oldValue = await _localSettingsService.ReadSettingAsync<string>("Language");
            if (value != oldValue)
            {
                IsLanguageChangedInfoBarOpen = true;
                await _localSettingsService.SaveSettingAsync("Language", value);
                await _localSettingsService.SaveSettingAsync("LanguageChanged", true);
            }
            else
            {
                IsLanguageChangedInfoBarOpen = false;
            }
        });
    }

    partial void OnIsUpdaterEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Updater.Enabled = value;

        SafeSaveBuilder();
    }

    partial void OnSelectedDaysBetweenUpdateCheckChanged(DaysBetweenUpdateCheck value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Updater.DaysBetweenUpdateCheck = value switch
        {
            DaysBetweenUpdateCheck.OneDay => 1,
            DaysBetweenUpdateCheck.ThreeDay => 3,
            DaysBetweenUpdateCheck.OneWeek => 7,
            DaysBetweenUpdateCheck.TwoWeeks => 14,
            _ => 1,
        };

        SafeSaveBuilder();
    }

    partial void OnIsCheckOnStartChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Updater.CheckOnStart = value;

        SafeSaveBuilder();
    }

    partial void OnIsAutoInstallChanged(bool value)
    {
        if (_isInitializing)
            return;

        if (!value)
        {
            IsUpdateSilent = false;
        }
        _builder.Config.Updater.AutoInstall = value;

        SafeSaveBuilder();
    }

    partial void OnIsUpdateSilentChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Updater.Silent = value;

        SafeSaveBuilder();
    }

    partial void OnSelectedUpdateChannelChanged(UpdateChannel value)
    {
        if (_isInitializing)
            return;

        if (value == UpdateChannel.Stable)
        {
            bool offerDowngrade = false;
            if (_builder.Config.Updater.VersionQueryUrl != null)
            {
                offerDowngrade = true;
            }
            _builder.Config.Updater.VersionQueryUrl = null;
            if (offerDowngrade)
            {
                Task.Run(async () =>
                {
                    _ = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.CheckForUpdate));
                    ApiResponse response = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.CheckForDowngradeNotify));
                    if (response.StatusCode == StatusCode.Downgrade)
                    {
                        UpdatesDate = "DowngradeAvailable".GetLocalized();
                    }
                });
            }
        }
        else
        {
            _builder.Config.Updater.VersionQueryUrl = @"https://raw.githubusercontent.com/AutoDarkMode/AutoDarkModeVersion/master/version-beta.yaml";
            _builder.Config.Updater.CheckOnStart = true;
            IsTunableDebug = true;
        }

        SafeSaveBuilder();
    }

    partial void OnIsAutostartChanged(bool value)
    {
        if (_isInitializing)
            return;

        ApiResponse result = new() { StatusCode = StatusCode.Err, Message = "error setting autostart entry" };
        if (value)
        {
            try
            {
                _builder.Config.Autostart.Validate = true;

                SafeSaveBuilder();

                _dispatcherQueue.TryEnqueue(async () =>
                {
                    result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.AddAutostart));
                    await GetAutostartInfo();
                    if (result.StatusCode != StatusCode.Ok)
                    {
                        throw new AddAutoStartException($"Could not add Auto Dark Mode to autostart", "AutoCheckBox_Checked");
                    }
                    await Task.Delay(800);
                });
            }
            catch (Exception ex)
            {
                IsAutostart = false;
                _errorService.ShowErrorMessageFromApi(result, ex, App.MainWindow.Content.XamlRoot);
            }
        }
        else
        {
            try
            {
                _builder.Config.Autostart.Validate = false;

                SafeSaveBuilder();

                _dispatcherQueue.TryEnqueue(async () =>
                {
                    result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RemoveAutostart));
                    await GetAutostartInfo();
                    if (result.StatusCode != StatusCode.Ok)
                    {
                        throw new AddAutoStartException($"Could not remove Auto Dark Mode to autostart", "AutoCheckBox_Checked");
                    }
                    await Task.Delay(800);
                });
            }
            catch (Exception ex)
            {
                IsAutostart = true;
                _errorService.ShowErrorMessageFromApi(result, ex, App.MainWindow.Content.XamlRoot);
            }
        }
    }

    partial void OnIsLoginWithTaskChanged(bool value)
    {
        if (_isInitializing)
            return;

        ApiResponse result = new() { StatusCode = StatusCode.Err };
        try
        {
            _builder.Config.Tunable.UseLogonTask = value;

            SafeSaveBuilder();

            if (_builder.Config.AutoThemeSwitchingEnabled)
            {
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.AddAutostart));
                    await GetAutostartInfo();
                    if (result.StatusCode != StatusCode.Ok)
                    {
                        _builder.Config.Tunable.UseLogonTask = value;
                        SafeSaveBuilder();
                        throw new AddAutoStartException($"error while processing CheckBoxLogonTask", "AutoDarkModeSvc.MessageParser.AddAutostart");
                    }
                    await Task.Delay(800);
                });
            }
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessageFromApi(result, ex, App.MainWindow.Content.XamlRoot);
        }
    }

    private void SafeSaveBuilder()
    {
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SettingsViewModel");
        }
    }
}
