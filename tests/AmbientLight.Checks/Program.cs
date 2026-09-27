using AdaptiveBrightness.Core;

if (args.Contains("--probe-brightness"))
{
    using var monitor = new MonitorController();
    for (var i = 0; i < 8; i++)
    {
        Console.WriteLine($"{DateTimeOffset.Now:O} VCP brightness={monitor.GetCurrentBrightnessPercent()?.ToString("F1") ?? "unavailable"}%");
        Thread.Sleep(1000);
    }
    return;
}

int checks = 0;
void Check(bool condition, string label)
{
    if (!condition) throw new InvalidOperationException(label);
    Console.WriteLine("PASS: " + label);
    checks++;
}
var settings = new AppSettings();
Check(CurveEditorMath.AxisMaximumLux([new(0, 0), new(20, 40)]) == 10000, "short curves retain a fixed 10,000 lux axis");
Check(CurveEditorMath.AxisMaximumLux([new(0, 0), new(20000, 100)]) == 10000, "axis never expands above 10,000 lux");
Check(!AppSettingsValidation.TryValidate(settings with { Curve = [new(0, 0), new(10001, 100)] }, out _), "points above 10,000 lux are rejected");
Check(CurveEditorMath.AxisTicks.Count == 38 && CurveEditorMath.AxisTicks.Contains(300) && CurveEditorMath.AxisTicks[^1] == 10000, "logarithmic axis has subdivisions through 10,000 lux");
Check(CurveEditorMath.LuxToNormalizedX(20000, 10000) == 1, "live readings above range stay inside the chart");
Check(!settings.BrightnessAutomationEnabled, "fresh installation does not start DDC writes");
using (var controller = new BrightnessController(settings))
{
    controller.Start();
    Check(controller.Status.Contains("已关闭"), "disabled controller does not open the serial port");
}
Check(Math.Abs(BrightnessCurve.Interpolate(Math.Sqrt(11 * 101) - 1, settings.Curve) - 38) < 1e-8, "curve interpolates in log lux space");
Check(BrightnessCurve.Interpolate(100000, settings.Curve) == 100, "curve saturates at its final point");
Check(!AppSettingsValidation.TryValidate(settings with { Curve = [new(10, 20), new(10, 60)] }, out _), "duplicate lux values cannot be saved");
Check(!AppSettingsValidation.TryValidate(settings with { Curve = [new(0, 20), new(double.NaN, 60)] }, out _), "NaN points cannot be saved");
Check(!AppSettingsValidation.TryValidate(settings with { Curve = [new(0, -1), new(10, 101)] }, out _), "brightness stays inside 0 to 100 percent");
var temp = Path.Combine(Path.GetTempPath(), "adm-ambient-" + Guid.NewGuid() + ".json");
try
{
    var curve = new List<CurvePoint> { new(0, 15), new(1000, 90) };
    var saved = settings with { Curve = curve, CurvePresets = [new("Desk", curve)], ActiveCurvePreset = "Desk", BrightnessAutomationEnabled = true };
    ConfigurationStore.SaveTo(temp, saved);
    var loaded = ConfigurationStore.LoadFrom(temp);
    Check(loaded.BrightnessAutomationEnabled && loaded.ActiveCurvePreset == "Desk" && loaded.Curve.SequenceEqual(curve), "curve, preset and independent switch survive reload");
    Check(loaded.MinimumWriteIntervalMilliseconds >= 500 && loaded.BrightnessRampPercentPerSecond == 2, "ramp and minimum write interval are preserved");
}
finally { if (File.Exists(temp)) File.Delete(temp); }
var firmware = Path.Combine(AppContext.BaseDirectory, "firmware.uf2");
Check(Uf2ImageValidator.TryValidateFile(firmware, out var info, out _) && info!.FamilyId == Uf2ImageValidator.Rp2040FamilyId, "bundled firmware is a valid RP2040 image");
var corrupt = File.ReadAllBytes(firmware);
corrupt[0] = 0;
Check(!Uf2ImageValidator.TryValidate(new MemoryStream(corrupt), out _, out _), "corrupt firmware is rejected before copy");
Check(Rp2040BootloaderIdentity.IsExpectedDrive("RPI-RP2", true, "Board-ID: RPI-RP2", true), "Pico bootloader is recognized");
Check(!Rp2040BootloaderIdentity.IsExpectedDrive("USB", true, "Board-ID: RPI-RP2", true), "unrelated removable disk is rejected");
Console.WriteLine($"{checks} checks passed; no firmware or display writes performed.");
