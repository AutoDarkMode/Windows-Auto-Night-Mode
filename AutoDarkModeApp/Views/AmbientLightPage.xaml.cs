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
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _autoSaveTimer;
    private bool _curveDirty;
    private bool _syncingPointEditors;

    public AmbientLightPage()
    {
        ViewModel = App.GetService<AutoSwitchViewModel>();
        _draftCurve = _settings.Curve.ToList();
        _draftPresets = _settings.CurvePresets.ToList();
        _selectedPresetName = _settings.ActiveCurvePreset;
        InitializeComponent();
        SaveStatusText.RegisterPropertyChangedCallback(TextBlock.TextProperty, (_, _) =>
            SaveStatusText.Visibility = string.IsNullOrWhiteSpace(SaveStatusText.Text) ? Visibility.Collapsed : Visibility.Visible);
        BrightnessAutomationSwitch.IsOn = _settings.BrightnessAutomationEnabled;
        RampSpeedNumberBox.Value = _settings.BrightnessRampPercentPerSecond;
        RebuildPresetSelector();
        SyncSelectedPointFields();
        LocalizeControls();
        _autoSaveTimer = DispatcherQueue.CreateTimer();
        _autoSaveTimer.Interval = TimeSpan.FromMilliseconds(250);
        _autoSaveTimer.Tick += (_, _) => FlushCurveChanges();
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
            FlushCurveChanges();
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

    private void QueueCurveChanges()
    {
        if (_initializingUi) return;
        _curveDirty = true;
        if (!_autoSaveTimer.IsRunning) _autoSaveTimer.Start();
    }

    private void RampSpeedNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_initializingUi) return;
        if (double.IsFinite(args.NewValue) && args.NewValue is >= 0.1 and <= 20) QueueCurveChanges();
    }

    private bool FlushCurveChanges()
    {
        _autoSaveTimer?.Stop();
        if (!_curveDirty) return true;
        if (!double.IsFinite(RampSpeedNumberBox.Value) || RampSpeedNumberBox.Value is < 0.1 or > 20) return false;
        var presets = _draftPresets.Select(p => string.Equals(p.Name, _selectedPresetName, StringComparison.OrdinalIgnoreCase)
            ? new NamedBrightnessCurve(p.Name, _draftCurve.ToList()) : p).ToList();
        var updated = _settings with { Curve = _draftCurve.ToList(), CurvePresets = presets, ActiveCurvePreset = _selectedPresetName,
            BrightnessRampPercentPerSecond = RampSpeedNumberBox.Value };
        if (!AppSettingsValidation.TryValidate(updated, out var error)) { SaveStatusText.Text = LocalizeStatus(error); return false; }
        try
        {
            ConfigurationStore.Save(updated);
            _settings = updated;
            _draftPresets = presets;
            _curveDirty = false;
            DeletePresetButton.IsEnabled = _draftPresets.Count > 1;
            SaveStatusText.Text = _uiLanguage.StartsWith("zh") ? "已自动保存" : "Saved automatically";
            return true;
        }
        catch (Exception ex) { SaveStatusText.Text = ex.Message; return false; }
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
        RampSpeedCard.Header = _uiLanguage.StartsWith("zh") ? "亮度变化速度（百分点/秒）" : "Brightness change speed (percentage points/s)";
        RampSpeedCard.Description = _uiLanguage.StartsWith("zh") ? "数值越大调节越快，修改立即生效。" : "Higher values change brightness faster. Changes apply immediately.";
        if (!_uiLanguage.StartsWith("zh"))
        {
            CurvePresetComboBox.Header = "Curve preset";
            SavePresetButton.Content = "Save preset as";
            DeletePresetButton.Content = "Delete preset";
            CurveTitleText.Text = "lux → DDC/CI brightness";
            CurveDescriptionText.Text = "Drag control points to adjust immediately. Horizontal: log10(1 + lux). Vertical: 0–100%.";
            SelectedLuxLabelText.Text = "Selected point · lux";
            SelectedBrightnessLabelText.Text = "Display brightness (0–100%)";
            RemoveCurvePointButton.Content = "Remove point";
            AddCurvePointButton.Content = "Add point";
            NewLuxTextBox.PlaceholderText = "New lux";
            NewBrightnessTextBox.PlaceholderText = "New brightness %";
        }
    }
}
