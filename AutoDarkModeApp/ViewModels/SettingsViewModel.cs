using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;
using Windows.Globalization;

namespace AutoDarkModeApp.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly Updater _updater;
    private readonly IErrorService _errorService;
    private readonly ILocalSettingsService _localSettingsService;

    [ObservableProperty]
    private bool _is12HourClock;
    [ObservableProperty]
    private bool _isHideTray;
    [ObservableProperty]
    private bool _isAlwaysFullDwmRefresh;
    [ObservableProperty]
    private bool _isTunableDebug;
    [ObservableProperty]
    private bool _isTunableTrace;

    [ObservableProperty]
    private bool _isUpdaterEnabled;
    [ObservableProperty]
    private string? _updatesDate;

    [ObservableProperty]
    private string? _language;

    [ObservableProperty]
    private int _selectIndexDaysBetweenUpdateCheck;
    [ObservableProperty]
    private bool _isCheckOnStart;
    [ObservableProperty]
    private bool _isAutoInstall;
    [ObservableProperty]
    private bool _isUpdateSilent;

    [ObservableProperty]
    private bool _isUpdatesChannelStable;
    [ObservableProperty]
    private bool _isUpdatesChannelBeta;

    [ObservableProperty]
    private bool _isAutostart;
    [ObservableProperty]
    private bool _isLoginWithTask;
    [ObservableProperty]
    private string? _autostartMode;
    [ObservableProperty]
    private string? _autostartPath;

    public ICommand RestartCommand
    {
        get;
    }

    public ICommand CheckUpdateCommand
    {
        get;
    }

    public ICommand AutostartRefreshCommand
    {
        get;
    }

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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
        }
        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();

        RestartCommand = new RelayCommand(() =>
        {
            MessageHandler.Client.SendMessageAndGetReply(Command.Restart);
            Process.Start(new ProcessStartInfo(Helper.ExecutionPathApp)
            {
                UseShellExecute = false,
                Verb = "open"
            });
            App.Current.Exit();
        });

        CheckUpdateCommand = new RelayCommand(() =>
        {
            UpdatesDate = "msgSearchUpd".GetLocalized();
            _updater.CheckNewVersion();
            if (_updater.UpdateAvailable())
            {
                UpdatesDate = "msgUpdateAvail".GetLocalized();
            }
            else
            {
                UpdatesDate = "msgNoUpd".GetLocalized();
            }
        });

        AutostartRefreshCommand = new RelayCommand(async () =>
        {
            await GetAutostartInfo();
        });
    }

    private void LoadSettings()
    {
        IsHideTray = !_builder.Config.Tunable.ShowTrayIcon;
        IsAlwaysFullDwmRefresh = _builder.Config.Tunable.AlwaysFullDwmRefresh;
        IsTunableDebug = _builder.Config.Tunable.Debug;
        IsTunableTrace = _builder.Config.Tunable.Trace;
        IsUpdaterEnabled = _builder.Config.Updater.Enabled;
        SelectIndexDaysBetweenUpdateCheck = _builder.Config.Updater.DaysBetweenUpdateCheck switch
        {
            1 => 0,
            3 => 1,
            7 => 2,
            14 => 3,
            _ => 0,
        };
        IsCheckOnStart = _builder.Config.Updater.CheckOnStart;
        IsAutoInstall = _builder.Config.Updater.AutoInstall;
        IsUpdateSilent = _builder.Config.Updater.Silent;
        IsLoginWithTask = _builder.Config.Tunable.UseLogonTask;

        if (string.IsNullOrEmpty(_builder.Config.Updater.VersionQueryUrl))
        {
            IsUpdatesChannelStable = true;
        }
        else if (_builder.Config.Updater.VersionQueryUrl.Equals(@"https://raw.githubusercontent.com/AutoDarkMode/AutoDarkModeVersion/master/version-beta.yaml"))
        {
            IsUpdatesChannelBeta = true;
        }
        else
        {
            IsUpdatesChannelStable = false;
            IsUpdatesChannelBeta = false;
        }
        if (_builder.UpdaterData.LastCheck.Year.ToString().Equals("1"))
        {
            UpdatesDate = "UpdatesTextBlockLastChecked".GetLocalized() + " " + "UpdatesTextBlockLastCheckedNever".GetLocalized();
        }
        else
        {
            UpdatesDate = "UpdatesTextBlockLastChecked".GetLocalized() + " " + _builder.UpdaterData.LastCheck;
        }

        _dispatcherQueue.TryEnqueue(async () =>
        {
            var twelveHourClock = await _localSettingsService.ReadSettingAsync<bool>("TwelveHourClock");
            if (!twelveHourClock)
            {
                Is12HourClock = false;
            }
            else
            {
                Is12HourClock = true;
            }
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

            await GetAutostartInfo();
        });
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
                    AutostartMode = "SettingsPageAutostartModeRegistry".GetLocalized();
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
                AutostartMode = "SettingsPageAutostartModeTask".GetLocalized();
                AutostartPath = autostartResponse.Details;
            }
            else if (autostartResponse.StatusCode == StatusCode.Disabled)
            {
                IsAutostart = false;
                AutostartMode = "disabled".GetLocalized();
                AutostartPath = "SettingsPageAutostartModeNone".GetLocalized();
            }
        }
        catch (Exception)
        {
            AutostartMode = "disabled".GetLocalized();
            AutostartPath = "SettingsPageAutostartModeNone".GetLocalized();
        }
    }

    private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            StateUpdateHandler.StopConfigWatcher();

            _builder.Load();
            _builder.LoadUpdaterData();
            LoadSettings();

            StateUpdateHandler.StartConfigWatcher();
        });
    }

    partial void OnIs12HourClockChanged(bool value)
    {
        if (value)
        {
            Task.Run(() => _localSettingsService.SaveSettingAsync("TwelveHourClock", true));
        }
        else
        {
            Task.Run(() => _localSettingsService.SaveSettingAsync("TwelveHourClock", false));
        }
    }

    partial void OnIsHideTrayChanged(bool value)
    {
        _builder.Config.Tunable.ShowTrayIcon = !value;
        _builder.Save();
    }

    partial void OnIsAlwaysFullDwmRefreshChanging(bool value)
    {
        if (value)
        {
            ContentDialog contentDialog = new()
            {
                Title = "SettingsPageCheckBoxAlwaysRefreshDwmHeader".GetLocalized(),
                Content = "SettingsPageCheckBoxAlwaysRefreshDwmExplanation".GetLocalized(),
                XamlRoot = App.MainWindow.Content.XamlRoot,
                CloseButtonText = "cancel".GetLocalized(),
                PrimaryButtonText = "ButtonConfirm".GetLocalized(),
            };
            _dispatcherQueue.TryEnqueue(async () =>
            {
                var result = await contentDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    IsAlwaysFullDwmRefresh = true;
                }
                else
                {
                    IsAlwaysFullDwmRefresh = false;
                }
            });
            _builder.Config.Tunable.AlwaysFullDwmRefresh = IsAlwaysFullDwmRefresh;
            _builder.Save();
        }
    }

    partial void OnIsTunableDebugChanged(bool value)
    {
        _builder.Config.Tunable.Debug = value;
        _builder.Save();
    }

    partial void OnIsTunableTraceChanged(bool value)
    {
        _builder.Config.Tunable.Trace = value;
        _builder.Save();
    }

    partial void OnLanguageChanged(string? value)
    {
        Task.Run(async () =>
        {
            if (value != null)
            {
                await _localSettingsService.SaveSettingAsync("Language", value);
            }
            else
            {
                await _localSettingsService.SaveSettingAsync("Language", "English (English)");
            }
        });
    }

    partial void OnIsUpdaterEnabledChanged(bool value)
    {
        _builder.Config.Updater.Enabled = value;
        _builder.Save();
    }

    partial void OnSelectIndexDaysBetweenUpdateCheckChanged(int value)
    {
        _builder.Config.Updater.DaysBetweenUpdateCheck = value switch
        {
            0 => 1,
            1 => 3,
            2 => 7,
            3 => 14,
            _ => 1,
        };
        _builder.Save();
    }

    partial void OnIsCheckOnStartChanged(bool value)
    {
        _builder.Config.Updater.CheckOnStart = value;
        _builder.Save();
    }

    partial void OnIsAutoInstallChanged(bool value)
    {
        if (!value)
        {
            IsUpdateSilent = false;
        }
        _builder.Config.Updater.AutoInstall = value;
        _builder.Save();
    }

    partial void OnIsUpdateSilentChanged(bool value)
    {
        _builder.Config.Updater.Silent = value;
        _builder.Save();
    }

    partial void OnIsUpdatesChannelStableChanged(bool value)
    {
        bool offerDowngrade = false;
        if (value)
        {
            if (_builder.Config.Updater.VersionQueryUrl != null)
            {
                offerDowngrade = true;
            }
            _builder.Config.Updater.VersionQueryUrl = null;
        }
        _builder.Save();
        if (offerDowngrade)
        {
            Task.Run(async () =>
            {
                _ = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.CheckForUpdate));
                ApiResponse response = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.CheckForDowngradeNotify));
                if (response.StatusCode == StatusCode.Downgrade)
                {
                    UpdatesDate = "SettingsPageDowngradeAvailable".GetLocalized();
                }
            });
        }
    }

    partial void OnIsUpdatesChannelBetaChanged(bool value)
    {
        if (value)
        {
            _builder.Config.Updater.VersionQueryUrl = @"https://raw.githubusercontent.com/AutoDarkMode/AutoDarkModeVersion/master/version-beta.yaml";
            _builder.Config.Updater.CheckOnStart = true;
            IsTunableDebug = true;
        }
        _builder.Save();
    }

    partial void OnIsAutostartChanged(bool value)
    {
        ApiResponse result = new()
        {
            StatusCode = StatusCode.Err,
            Message = "error setting autostart entry"
        };
        if (value)
        {
            try
            {
                _builder.Config.Autostart.Validate = true;
                _builder.Save();
                Task.Run(async () =>
                {
                    result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.AddAutostart));
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
                _builder.Save();
                Task.Run(async () =>
                {
                    result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RemoveAutostart));
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
        ApiResponse result = new() { StatusCode = StatusCode.Err };
        try
        {
            _builder.Config.Tunable.UseLogonTask = value;
            _builder.Save();

            if (_builder.Config.AutoThemeSwitchingEnabled)
            {
                Task.Run(async () =>
                {
                    result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.AddAutostart));
                    if (result.StatusCode != StatusCode.Ok)
                    {
                        _builder.Config.Tunable.UseLogonTask = value;
                        _builder.Save();
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

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
