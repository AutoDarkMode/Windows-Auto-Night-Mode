using System;
using System.IO;
using AdaptiveBrightness.Core;

namespace AutoDarkModeSvc;

/// <summary>Owns CDC/DDC for the lifetime of the tray service, independently of the settings window.</summary>
internal sealed class AmbientBrightnessHost : IDisposable
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 2000 };
    private BrightnessController controller;
    private string lastConfiguration;

    public AmbientBrightnessHost()
    {
        timer.Tick += (_, _) => Refresh();
        Refresh();
        timer.Start();
    }

    private void Refresh()
    {
        try
        {
            string path = ConfigurationStore.ConfigurationPath;
            string json = File.Exists(path) ? File.ReadAllText(path) : "";
            if (json != lastConfiguration)
            {
                var settings = json.Length == 0 ? new AppSettings() : System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                if (settings == null || !AppSettingsValidation.TryValidate(settings, out var error))
                    throw new InvalidDataException("Invalid ambient brightness configuration");
                controller?.Dispose();
                controller = new BrightnessController(settings);
                controller.Start();
                lastConfiguration = json;
            }
            WriteStatus(controller?.Status ?? "自动亮度调节已关闭");
        }
        catch (Exception ex)
        {
            // A malformed config never starts a replacement controller with unsafe defaults.
            NLog.LogManager.GetCurrentClassLogger().Warn(ex, "ambient brightness update failed");
            WriteStatus(ex.Message);
        }
    }

    private static void WriteStatus(string status)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigurationStore.ConfigurationPath));
            File.WriteAllText(ConfigurationStore.ConfigurationPath + ".status", status);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public void Dispose()
    {
        timer.Stop();
        timer.Dispose();
        controller?.Dispose();
    }
}
