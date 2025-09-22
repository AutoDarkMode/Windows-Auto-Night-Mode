using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using Microsoft.Windows.Globalization;


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
    public partial bool IsDwmRefreshViaColorization { get; set; }

    [ObservableProperty]
    public partial bool IsHideTray { get; set; }

    [ObservableProperty]
    public partial bool IsTunableDebug { get; set; }

    [ObservableProperty]
    public partial bool IsTunableTrace { get; set; }

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
            Microsoft.UI.Xaml.Application.Current.Exit();
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
        IsDwmRefreshViaColorization = _builder.Config.Tunable.DwmRefreshViaColorization;
        IsTunableDebug = _builder.Config.Tunable.Debug;
        IsTunableTrace = _builder.Config.Tunable.Trace;
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
            if (string.IsNullOrEmpty(languageText))
            {
                var preferredLanguages = ApplicationLanguages.Languages;
                string topLanguage = preferredLanguages.FirstOrDefault(); // e.g., "fr-FR"
                if (LanguageConstants.SupportedCultures.Contains(topLanguage))
                {
                    Language = topLanguage;
                }
                else
                {
                    var neutralLanguage = topLanguage.Split('-')[0]; // e.g., "fr"
                    if (LanguageConstants.SupportedCultures.Contains(neutralLanguage))
                    {
                        Language = neutralLanguage;
                    }
                    else
                    {
                        Language = "en";
                    }
                }
                await _localSettingsService.SaveSettingAsync("Language", Language);
            }
            else
            {
                Language = languageText;
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
                else if (autostartResponse.Message == "Disabled")
                {
                    ContentDialog contentDialog = new()
                    {
                        Title = "StartWithWindows".GetLocalized(),
                        Content = "StartWithWindowsFailed_Content".GetLocalized(),
                        XamlRoot = App.MainWindow.Content.XamlRoot,
                        CloseButtonText = "Confirm".GetLocalized(),
                    };

                    if (IsAutostart)
                        await contentDialog.ShowAsync();

                    IsAutostart = false;
                    AutostartMode = "Disabled".GetLocalized();
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

    partial void OnLanguageChanged(string? value)
    {
        if (_isInitializing)
            return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            var currentCulture = System.Globalization.CultureInfo.CurrentUICulture;
            bool isSameLanguage = string.Equals(value, currentCulture.Name, StringComparison.OrdinalIgnoreCase);

            if (!isSameLanguage)
            {
                IsLanguageChangedInfoBarOpen = true;
                _localSettingsService.SaveSettingAsync("Language", value);
                _localSettingsService.SaveSettingAsync("LanguageChanged", true);
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


    partial void OnIsDwmRefreshViaColorizationChanged(bool value)
    {
        if (_isInitializing)
            return;

        if (value)
        {
            ContentDialog contentDialog = new()
            {
                Title = "AggressiveDwmRefresh_Title".GetLocalized(),
                Content = "AlwaysRefreshDwm_Content".GetLocalized(),
                XamlRoot = App.MainWindow.Content.XamlRoot,
                CloseButtonText = "Cancel".GetLocalized(),
                PrimaryButtonText = "Confirm".GetLocalized(),
            };
            _dispatcherQueue.TryEnqueue(async () =>
            {
                var result = await contentDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    IsDwmRefreshViaColorization = true;
                }
                else
                {
                    IsDwmRefreshViaColorization = false;
                }
            });
            _builder.Config.Tunable.DwmRefreshViaColorization = IsDwmRefreshViaColorization;
        }
        else
        {
            _builder.Config.Tunable.DwmRefreshViaColorization = IsDwmRefreshViaColorization;
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
        else if (!AutostartMode?.Equals("Disabled".GetLocalized()) == true)
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


public partial class SettingsViewModel
{
    public ObservableCollection<LanguageOption> LanguageOptions { get; } =
        [
            new() { DisplayName = "Bahasa Indonesia (Indonesian)", CultureCode = "id" },
            new() { DisplayName = "Česky (Czech)", CultureCode = "cs" },
            new() { DisplayName = "Deutsch (German)", CultureCode = "de" },
            new() { DisplayName = "English (English)", CultureCode = "en" },
            new() { DisplayName = "Español (Spanish)", CultureCode = "es" },
            new() { DisplayName = "Français (French)", CultureCode = "fr" },
            new() { DisplayName = "Italiano (Italian)", CultureCode = "it" },
            new() { DisplayName = "Magyarul (Hungarian)", CultureCode = "hu" },
            new() { DisplayName = "Nederlands (Dutch)", CultureCode = "nl" },
            new() { DisplayName = "Norsk Bokmål (Norwegian Bokmål)", CultureCode = "nb" },
            new() { DisplayName = "فارسی (Persian)", CultureCode = "fa" },
            new() { DisplayName = "Polski (Polish)", CultureCode = "pl" },
            new() { DisplayName = "Português (Brazilian Portuguese)", CultureCode = "pt-BR" },
            new() { DisplayName = "Português (European Portuguese)", CultureCode = "pt-PT" },
            new() { DisplayName = "Română (Romanian)", CultureCode = "ro" },
            new() { DisplayName = "Ελληνικά (Greek)", CultureCode = "el" },
            new() { DisplayName = "Русский (Russian)", CultureCode = "ru" },
            new() { DisplayName = "Srpski (Serbian)", CultureCode = "sr" },
            new() { DisplayName = "Türkçe (Turkish)", CultureCode = "tr" },
            new() { DisplayName = "Українська (Ukrainian)", CultureCode = "uk" },
            new() { DisplayName = "Tiếng Việt (Vietnamese)", CultureCode = "vi" },
            new() { DisplayName = "日本語 (Japanese)", CultureCode = "ja" },
            new() { DisplayName = "简体中文 (Chinese Simplified)", CultureCode = "zh-Hans" },
            new() { DisplayName = "繁體中文 (Chinese Traditional)", CultureCode = "zh-Hant" },
        ];
}

public partial class LanguageViewModel : INotifyPropertyChanged
{
    public ObservableCollection<LanguageOption> LanguageOptions { get; set; }
    private string _selectedLanguage;

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value)
            {
                _selectedLanguage = value;
                OnPropertyChanged(nameof(SelectedLanguage));
            }
        }
    }

    public LanguageViewModel()
    {
        LanguageOptions = new ObservableCollection<LanguageOption>(GetHybridLanguageNames());
        SelectedLanguage = LanguageOptions.FirstOrDefault()?.CultureCode
            ?? LanguageOptions.FirstOrDefault()?.CultureCode
            ?? string.Empty;
    }

    private static ObservableCollection<LanguageOption> GetHybridLanguageNames()
    {
        return new ObservableCollection<LanguageOption>(
            LanguageConstants.SupportedCultures.Select(code =>
            {
                var culture = CultureInfo.GetCultureInfo(code);
                string native = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(culture.NativeName);
                string english = culture.EnglishName.Split('(')[0].Trim();
                return new LanguageOption
                {
                    DisplayName = $"{native} ({english})",
                    CultureCode = code
                };
            })
           );
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class LanguageOption
{
    public required string DisplayName { get; set; }
    public required string CultureCode { get; set; }
}

public static class LanguageConstants
{
    public static readonly string[] SupportedCultures = new[]
    {
        "id", "cs", "de", "en", "es", "fr", "it", "hu", "nl", "nb",
        "fa", "pl", "pt-BR", "pt-PT", "ro", "sr", "vi", "tr", "el",
        "ru", "uk", "ja", "zh-Hans", "zh-Hant"
    };
}