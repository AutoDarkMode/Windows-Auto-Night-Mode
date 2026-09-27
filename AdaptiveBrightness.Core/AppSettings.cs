using System.Text.Json;
using Microsoft.Win32;

namespace AdaptiveBrightness.Core;

public sealed record CurvePoint(double Lux, double Brightness);
public sealed record NamedBrightnessCurve(string Name, List<CurvePoint> Points);

public sealed record AppSettings
{
    public string SerialPort { get; init; } = "";
    public int BaudRate { get; init; } = 115200;
    public bool DryRun { get; init; }
    public int PollIntervalMilliseconds { get; init; } = 250;
    public int MinimumWriteIntervalMilliseconds { get; init; } = 500;
    public double ChangeDeadbandPercent { get; init; } = 0.25;
    public double BrightnessRampPercentPerSecond { get; init; } = 4;
    public bool BrightnessAutomationEnabled { get; init; } = false;
    public List<CurvePoint> Curve { get; init; } =
    [
        new(0, 0), new(10, 26), new(100, 50), new(1000, 75), new(10000, 100)
    ];
    public List<NamedBrightnessCurve> CurvePresets { get; init; } = [];
    public string ActiveCurvePreset { get; init; } = "";
    public string Language { get; init; } = "System";
    public bool StartOnSignIn { get; init; }
    public bool ThemeAutomationEnabled { get; init; }
    public double DarkThemeBelowLux { get; init; } = 30;
    public double LightThemeAboveLux { get; init; } = 80;
    public bool WallpaperSwitchEnabled { get; init; }
    public string LightWallpaperPath { get; init; } = "";
    public string DarkWallpaperPath { get; init; } = "";
    public string LightWallpaperPosition { get; init; } = "Fill";
    public string DarkWallpaperPosition { get; init; } = "Fill";
    public bool AccentColorSwitchEnabled { get; init; }
    public string AccentColorMode { get; init; } = "Automatic";
    public string LightAccentColor { get; init; } = "#0078D4";
    public string DarkAccentColor { get; init; } = "#0078D4";
    public bool CursorSchemeSwitchEnabled { get; init; }
    public string LightCursorScheme { get; init; } = "Windows Aero";
    public string DarkCursorScheme { get; init; } = "Windows Black";

    public string? ResolveSerialPort()
    {
        if (!string.IsNullOrWhiteSpace(SerialPort)) return SerialPort;
        try
        {
            using var usb = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
            if (usb is null) return null;
            foreach (var deviceName in usb.GetSubKeyNames().Where(name => name.StartsWith("VID_2E8A&PID_10C1&MI_00", StringComparison.OrdinalIgnoreCase)))
            using (var device = usb.OpenSubKey(deviceName))
            {
                if (device is null) continue;
                foreach (var instanceName in device.GetSubKeyNames())
                using (var instance = device.OpenSubKey(instanceName))
                using (var parameters = instance?.OpenSubKey("Device Parameters"))
                {
                    var port = parameters?.GetValue("PortName") as string;
                    if (!string.IsNullOrWhiteSpace(port) && System.IO.Ports.SerialPort.GetPortNames().Contains(port, StringComparer.OrdinalIgnoreCase)) return port;
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException) { }
        return null;
    }
}

public static class AppSettingsValidation
{
    public static bool TryValidate(AppSettings settings, out string error)
    {
        if (!double.IsFinite(settings.BrightnessRampPercentPerSecond) || settings.BrightnessRampPercentPerSecond is < 0.1 or > 20)
            return Fail("亮度变化速度必须在每秒 0.1 到 20 个百分点之间。", out error);
        if (!double.IsFinite(settings.DarkThemeBelowLux) || settings.DarkThemeBelowLux < 0)
            return Fail("深色阈值必须是非负有限数值。", out error);
        if (!double.IsFinite(settings.LightThemeAboveLux) || settings.LightThemeAboveLux <= settings.DarkThemeBelowLux)
            return Fail("浅色阈值必须是有限数值，并且高于深色阈值。", out error);
        if (!TryValidateCurve(settings.Curve, out error)) return false;
        if (settings.CurvePresets is null) return Fail("亮度曲线预设列表无效。", out error);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in settings.CurvePresets)
        {
            if (preset is null || string.IsNullOrWhiteSpace(preset.Name) || preset.Name.Trim().Length > 64)
                return Fail("预设名称不能为空，且不能超过 64 个字符。", out error);
            if (!names.Add(preset.Name.Trim())) return Fail("预设名称不能重复。", out error);
            if (!TryValidateCurve(preset.Points, out error)) return false;
        }
        if (settings.CurvePresets.Count > 0 && !names.Contains(settings.ActiveCurvePreset ?? ""))
            return Fail("当前曲线预设不存在。", out error);
        if (!new[] { "System", "zh-CN", "en-US" }.Contains(settings.Language, StringComparer.OrdinalIgnoreCase))
            return Fail("界面语言仅支持跟随系统、简体中文或 English。", out error);
        if (!new[] { "Fill", "Fit", "Stretch", "Tile", "Center" }.Contains(settings.LightWallpaperPosition, StringComparer.Ordinal) ||
            !new[] { "Fill", "Fit", "Stretch", "Tile", "Center" }.Contains(settings.DarkWallpaperPosition, StringComparer.Ordinal))
            return Fail("壁纸位置模式无效。", out error);
        if (!new[] { "Automatic", "Manual" }.Contains(settings.AccentColorMode, StringComparer.Ordinal) ||
            !IsHexColor(settings.LightAccentColor) || !IsHexColor(settings.DarkAccentColor))
            return Fail("强调色设置无效。", out error);

        error = "";
        return true;
    }

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }

    private static bool TryValidateCurve(IReadOnlyCollection<CurvePoint>? curve, out string error)
    {
        if (curve is null || curve.Count < 2) return Fail("亮度曲线至少需要两个点。", out error);
        if (curve.Any(point => point is null)) return Fail("亮度曲线包含无效控制点。", out error);
        var sorted = curve.OrderBy(point => point.Lux).ToArray();
        for (var i = 0; i < sorted.Length; i++)
        {
            if (!double.IsFinite(sorted[i].Lux) || sorted[i].Lux < 0 || sorted[i].Lux > CurveEditorMath.MaximumLux) return Fail("曲线 lux 必须在 0 到 10,000 之间。", out error);
            if (!double.IsFinite(sorted[i].Brightness) || sorted[i].Brightness is < 0 or > 100) return Fail("曲线亮度必须在 0 到 100 之间。", out error);
            if (i > 0 && sorted[i - 1].Lux >= sorted[i].Lux) return Fail("曲线 lux 必须互不相同。", out error);
        }
        error = "";
        return true;
    }

    private static bool IsHexColor(string? value) => value is { Length: 7 } && value[0] == '#' &&
        uint.TryParse(value.AsSpan(1), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out _);
}

public static class BrightnessCurve
{
    public static double Interpolate(double lux, IReadOnlyCollection<CurvePoint> points)
    {
        if (!double.IsFinite(lux) || lux < 0) throw new ArgumentOutOfRangeException(nameof(lux));
        var curve = points.OrderBy(point => point.Lux).ToArray();
        if (curve.Length == 0) return 50;
        if (lux <= curve[0].Lux) return curve[0].Brightness;
        for (var i = 1; i < curve.Length; i++)
        {
            if (lux > curve[i].Lux) continue;
            var a = curve[i - 1];
            var b = curve[i];
            var position = Math.Log10(1 + lux);
            var start = Math.Log10(1 + a.Lux);
            var end = Math.Log10(1 + b.Lux);
            return a.Brightness + (position - start) / (end - start) * (b.Brightness - a.Brightness);
        }
        return curve[^1].Brightness;
    }
}

public sealed class ThemeThresholdPolicy(bool initialIsLight)
{
    public bool IsLight { get; private set; } = initialIsLight;

    public bool Observe(double lux, double darkThreshold, double lightThreshold)
    {
        if (!double.IsFinite(lux) || lux < 0) throw new ArgumentOutOfRangeException(nameof(lux));
        if (!double.IsFinite(darkThreshold) || darkThreshold < 0 ||
            !double.IsFinite(lightThreshold) || lightThreshold <= darkThreshold)
            throw new ArgumentOutOfRangeException(nameof(lightThreshold), "Light threshold must exceed a non-negative dark threshold.");

        if (IsLight && lux <= darkThreshold) IsLight = false;
        else if (!IsLight && lux >= lightThreshold) IsLight = true;
        return IsLight;
    }
}

public static class ConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    public static string ConfigurationPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoDarkMode", "ambient-brightness.json");

    public static AppSettings Load(string? legacyPath = null) => LoadFrom(ConfigurationPath, legacyPath);

    public static AppSettings LoadFrom(string configurationPath, string? legacyPath = null)
    {
        try
        {
            if (File.Exists(configurationPath))
                return LoadAndValidate(File.ReadAllText(configurationPath));
            if (legacyPath is not null && File.Exists(legacyPath))
                return LoadAndValidate(File.ReadAllText(legacyPath));
        }
        catch (JsonException) { }
        catch (IOException) { }
        return PrepareSettings(new AppSettings());
    }

    private static AppSettings LoadAndValidate(string json)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        if (settings.Curve is null || settings.Curve.Count < 2) return PrepareSettings(new AppSettings());
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty(nameof(AppSettings.CurvePresets), out _) || settings.CurvePresets is not { Count: > 0 })
            settings = settings with
            {
                CurvePresets = [new NamedBrightnessCurve("默认", settings.Curve.ToList())],
                ActiveCurvePreset = "默认"
            };
        else if (string.IsNullOrWhiteSpace(settings.ActiveCurvePreset) ||
                 !settings.CurvePresets.Any(preset => string.Equals(preset.Name, settings.ActiveCurvePreset, StringComparison.OrdinalIgnoreCase)))
            settings = settings with { ActiveCurvePreset = settings.CurvePresets[0].Name };
        settings = PrepareSettings(settings);
        return AppSettingsValidation.TryValidate(settings, out _) ? settings : PrepareSettings(new AppSettings());
    }

    private static AppSettings PrepareSettings(AppSettings settings)
    {
        if (settings.Curve is null || settings.Curve.Count < 2) settings = new AppSettings();
        return settings.CurvePresets is { Count: > 0 }
            ? settings
            : settings with
            {
                CurvePresets = [new NamedBrightnessCurve("默认", settings.Curve.ToList())],
                ActiveCurvePreset = "默认"
            };
    }

    public static void Save(AppSettings settings) => SaveTo(ConfigurationPath, settings);

    public static void SaveTo(string configurationPath, AppSettings settings)
    {
        var path = Path.GetFullPath(configurationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }
}

public static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AdaptiveBrightness";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(ValueName) is string command && command.Contains("AdaptiveBrightness.WinUI", StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled, string executablePath)
    {
        if (enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            key.SetValue(ValueName, $"\"{executablePath}\" --background");
            return;
        }

        using var existingKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        existingKey?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
