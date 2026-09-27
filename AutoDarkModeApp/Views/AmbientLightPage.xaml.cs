using System.ComponentModel;
using System.Globalization;
using System.IO;
using AdaptiveBrightness.Core;
using AutoDarkModeApp.ViewModels;

namespace AutoDarkModeApp.Views;

public sealed partial class AmbientLightPage : Page
{
    public AutoSwitchViewModel ViewModel { get; }
    private AppSettings _settings = ConfigurationStore.Load();
    private List<CurvePoint> _draftCurve;
    private List<NamedBrightnessCurve> _draftPresets;
    private string _selectedPresetName;
    private int _selectedPointIndex;
    private bool _initializingUi = true;
    private bool _draggingPoint;
    private uint _dragPointerId;
    private double? _lastLux;
    private readonly string _uiLanguage = LanguageHelper.SelectedLanguageCode ?? CultureInfo.CurrentUICulture.Name;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _statusTimer;

    public AmbientLightPage()
    {
        ViewModel = App.GetService<AutoSwitchViewModel>();
        _draftCurve = _settings.Curve.ToList();
        _draftPresets = _settings.CurvePresets.ToList();
        _selectedPresetName = _settings.ActiveCurvePreset;
        InitializeComponent();
        BrightnessAutomationSwitch.IsOn = _settings.BrightnessAutomationEnabled;
        RebuildPresetSelector();
        SyncSelectedPointFields();
        LocalizeControls();
        _initializingUi = false;
        _statusTimer = DispatcherQueue.CreateTimer();
        _statusTimer.Interval = TimeSpan.FromSeconds(2);
        _statusTimer.Tick += (_, _) => ReadControllerStatus();
        Loaded += (_, _) =>
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.PropertyChanged += OnReadingChanged;
            RefreshPreview();
            DrawCurve();
            ReadControllerStatus();
            _statusTimer.Start();
        };
        Unloaded += (_, _) =>
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.PropertyChanged -= OnReadingChanged;
            _statusTimer.Stop();
        };
        ActualThemeChanged += (_, _) => DrawCurve();
    }

    private void OnReadingChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ViewModel.CurrentLuxReading) or nameof(ViewModel.HasCurrentLuxReading)) RefreshPreview();
    }

    private void RefreshPreview()
    {
        _lastLux = ViewModel.HasCurrentLuxReading ? ViewModel.CurrentLuxReading : null;
        DrawCurve();
    }

    private void ReadControllerStatus()
    {
        if (!_settings.BrightnessAutomationEnabled)
        {
            ControllerStatusText.Text = _uiLanguage.StartsWith("zh") ? "自动亮度调节已关闭" : "Automatic brightness is off";
            return;
        }
        var path = ConfigurationStore.ConfigurationPath + ".status";
        try
        {
            ControllerStatusText.Text = File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < TimeSpan.FromSeconds(10)
                ? LocalizeStatus(File.ReadAllText(path))
                : (_uiLanguage.StartsWith("zh") ? "自动亮度后台尚未连接，请运行此版本配套的 AutoDarkModeSvc。" : "Brightness control is not connected. Start the AutoDarkModeSvc supplied with this version.");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void BrightnessAutomationSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializingUi) return;
        try
        {
            var updated = _settings with { BrightnessAutomationEnabled = BrightnessAutomationSwitch.IsOn };
            ConfigurationStore.Save(updated);
            _settings = updated;
            ReadControllerStatus();
        }
        catch (Exception ex)
        {
            _initializingUi = true;
            BrightnessAutomationSwitch.IsOn = _settings.BrightnessAutomationEnabled;
            _initializingUi = false;
            SaveStatusText.Text = ex.Message;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCommitSelectedEditor(out var error)) { SaveStatusText.Text = error; return; }
        var presets = _draftPresets.Select(p => string.Equals(p.Name, _selectedPresetName, StringComparison.OrdinalIgnoreCase)
            ? new NamedBrightnessCurve(p.Name, _draftCurve.ToList()) : p).ToList();
        var updated = _settings with { Curve = _draftCurve.ToList(), CurvePresets = presets, ActiveCurvePreset = _selectedPresetName };
        if (!AppSettingsValidation.TryValidate(updated, out error)) { SaveStatusText.Text = LocalizeStatus(error); return; }
        try
        {
            ConfigurationStore.Save(updated);
            _settings = updated;
            _draftPresets = presets;
            DeletePresetButton.IsEnabled = false;
            SaveStatusText.Text = _uiLanguage.StartsWith("zh") ? "曲线已保存，后台服务将在 2 秒内应用。" : "Curve saved. The background service will apply it within 2 seconds.";
        }
        catch (Exception ex) { SaveStatusText.Text = ex.Message; }
    }

    private string T(string key) => UiText.Get(key, _uiLanguage);
    private string LocalizeStatus(string status) => _uiLanguage.StartsWith("zh") ? status : UiText.TranslateStatus(status);
    private static ComboBoxItem? ItemByTag(ComboBox box, string tag) => box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => string.Equals(i.Tag as string, tag, StringComparison.OrdinalIgnoreCase));
    private static bool TryParseNumber(string text, out double value) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    private static string FormatNumber(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private void LocalizeControls()
    {
        BrightnessSectionTitle.Text = T("BrightnessPageTitle");
        BrightnessSwitchCard.Header = _uiLanguage.StartsWith("zh") ? "启用自动亮度调节" : "Enable automatic brightness";
        BrightnessSwitchCard.Description = _uiLanguage.StartsWith("zh") ? "通过 Pico USB CDC 调节外接显示器亮度，与主题切换独立。" : "Adjust external displays using Pico USB CDC, independently of theme switching.";
        FirmwareSectionTitle.Text = T("FirmwareTitle");
        FirmwareCard.Header = T("FirmwareTitle");
        FirmwareCard.Description = T("FirmwareInstructions");
        FlashFirmwareButton.Content = T("FirmwareButton");
        ApplyCurveButton.Content = _uiLanguage.StartsWith("zh") ? "应用并保存" : "Apply and save";
        if (!_uiLanguage.StartsWith("zh"))
        {
            CurvePresetComboBox.Header = "Curve preset";
            SavePresetButton.Content = "Save preset as";
            DeletePresetButton.Content = "Delete preset";
            CurveTitleText.Text = "lux → DDC/CI brightness";
            CurveDescriptionText.Text = "Drag control points to preview. Horizontal: log10(1 + lux). Vertical: 0–100%.";
            SelectedLuxLabelText.Text = "Selected point · lux";
            SelectedBrightnessLabelText.Text = "Display brightness (0–100%)";
            RemoveCurvePointButton.Content = "Remove point";
            AddCurvePointButton.Content = "Add point";
            NewLuxTextBox.PlaceholderText = "New lux";
            NewBrightnessTextBox.PlaceholderText = "New brightness %";
            CurveSafetyText.Text = "Edits are previews until you select Apply and save.";
        }
    }
}
