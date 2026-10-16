#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace AutoDarkModeLib;

/// <summary>
/// An opened webcam with a Bgra8 frame reader on its colour stream, disposing it releases the camera
/// </summary>
public sealed class WebcamSession(MediaCapture capture, MediaFrameReader reader) : IDisposable
{
    public MediaCapture Capture { get; } = capture;
    public MediaFrameReader Reader { get; } = reader;

    public void Dispose()
    {
        Reader.Dispose();
        Capture.Dispose();
    }
}

/// <summary>
/// Estimates the ambient light level in lux from the webcam or the weather, used by the service and the app preview
/// </summary>
public static class AmbientLightEstimator
{
    // Open-Meteo updates its current weather every 15 minutes
    public const int MinimumWeatherIntervalMin = 15;
    // gives the webcam auto exposure time to settle
    public const int WebcamWarmupMs = 2000;

    // webcams are no light meters, this only gives a rough estimate to tune the thresholds against
    private const double WebcamLuxPerLuminanceSecond = 12;
    private const double WebcamDefaultExposureSeconds = 1.0 / 30;
    // about 120 lux per W/m^2 outdoors, and a room near a window gets about 1% of it
    private const double WeatherLuxPerWattPerSquareMeter = 1.2;
    // the mean luminance needs no detail, a small frame keeps the per-frame conversion and copy cheap
    private static readonly BitmapSize WebcamFrameSize = new() { Width = 160, Height = 120 };

    private static readonly HttpClient WeatherClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly object WeatherLock = new();
    private static DateTime _lastWeatherRequest;
    private static (double Lat, double Lon) _lastWeatherLocation;
    private static double? _lastWeatherLux;

    /// <summary>
    /// Whether a light level can be compared against the thresholds
    /// </summary>
    public static bool IsValidLux(double lux) => double.IsFinite(lux) && lux >= 0;

    public static async Task<MediaFrameSourceGroup[]> FindWebcamsAsync()
    {
        var groups = await MediaFrameSourceGroup.FindAllAsync();
        return groups.Where(g => g.SourceInfos.Any(s => s.SourceKind == MediaFrameSourceKind.Color)).ToArray();
    }

    /// <summary>
    /// Returns the webcam with the given id, or the first webcam if it is not connected
    /// </summary>
    public static async Task<MediaFrameSourceGroup> FindWebcamAsync(string id)
    {
        return SelectWebcam(await FindWebcamsAsync(), id);
    }

    /// <summary>
    /// Returns the webcam with the given id from the found webcams, or the first webcam if it is not among them
    /// </summary>
    public static MediaFrameSourceGroup SelectWebcam(MediaFrameSourceGroup[] webcams, string id)
    {
        return Array.Find(webcams, g => g.Id == id) ?? webcams.FirstOrDefault();
    }

    /// <summary>
    /// Opens the webcam in shared read only mode, so other apps using it are not blocked. The reader still has to be started.
    /// </summary>
    public static async Task<WebcamSession> OpenWebcamAsync(MediaFrameSourceGroup group)
    {
        MediaCapture capture = new();
        try
        {
            await capture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                SourceGroup = group,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            });
            var source = capture.FrameSources.Values.FirstOrDefault(s => s.Info.SourceKind == MediaFrameSourceKind.Color)
                ?? throw new InvalidOperationException("webcam has no colour stream");
            var reader = await capture.CreateFrameReaderAsync(source, MediaEncodingSubtypes.Bgra8, WebcamFrameSize);
            return new(capture, reader);
        }
        catch
        {
            capture.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Returns the light level of a webcam frame, or null if the frame holds no image
    /// </summary>
    public static double? EstimateWebcamLux(WebcamSession webcam, MediaFrameReference frame)
    {
        SoftwareBitmap frameBitmap = frame?.VideoMediaFrame?.SoftwareBitmap;
        bool hasImage = frameBitmap != null && frameBitmap.PixelWidth > 0 && frameBitmap.PixelHeight > 0;
        if (!hasImage) return null;

        // some drivers ignore the requested format
        bool isBgra8 = frameBitmap.BitmapPixelFormat == BitmapPixelFormat.Bgra8;
        using SoftwareBitmap convertedBitmap = isBgra8 ? null : SoftwareBitmap.Convert(frameBitmap, BitmapPixelFormat.Bgra8);
        SoftwareBitmap bitmap = convertedBitmap ?? frameBitmap;

        // mean Rec. 709 luminance from 0 to 1
        byte[] pixels = new byte[4 * bitmap.PixelWidth * bitmap.PixelHeight];
        bitmap.CopyToBuffer(pixels.AsBuffer());
        double sum = 0;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            sum += 0.0722 * pixels[i] + 0.7152 * pixels[i + 1] + 0.2126 * pixels[i + 2];
        }
        double luminance = sum / (pixels.Length / 4) / 255;
        return WebcamLuxPerLuminanceSecond * luminance / GetExposureSeconds(webcam);
    }

    // auto exposure brightens dark scenes, so the exposure time (log2 seconds) tells a dim room from a bright one
    private static double GetExposureSeconds(WebcamSession webcam)
    {
        try
        {
            bool hasPlausibleExposure = webcam.Capture.VideoDeviceController.Exposure.TryGetValue(out double exposure) && exposure >= -14 && exposure <= 1;
            if (hasPlausibleExposure) return Math.Pow(2, exposure);
        }
        catch (Exception)
        {
            // some drivers throw instead of reporting no exposure
        }
        return WebcamDefaultExposureSeconds;
    }

    /// <summary>
    /// Returns the light level from the current solar radiation at the location reported by Open-Meteo.
    /// Open-Meteo is asked at most every <see cref="MinimumWeatherIntervalMin"/> minutes, failed requests included.
    /// </summary>
    public static async Task<double> FetchWeatherLuxAsync(double lat, double lon)
    {
        bool isValidLocation = Math.Abs(lat) <= 90 && Math.Abs(lon) <= 180;
        if (!isValidLocation) throw new ArgumentOutOfRangeException(nameof(lat), $"invalid location {lat}, {lon}");

        lock (WeatherLock)
        {
            bool isRecentRequestForLocation = _lastWeatherLocation == (lat, lon) && DateTime.Now - _lastWeatherRequest < TimeSpan.FromMinutes(MinimumWeatherIntervalMin);
            if (isRecentRequestForLocation)
            {
                return _lastWeatherLux ?? throw new InvalidOperationException("last weather request failed");
            }
            _lastWeatherRequest = DateTime.Now;
            _lastWeatherLocation = (lat, lon);
            _lastWeatherLux = null;
        }

        string url = string.Format(CultureInfo.InvariantCulture, "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=shortwave_radiation", lat, lon);
        string data = await WeatherClient.GetStringAsync(url);
        using JsonDocument json = JsonDocument.Parse(data);
        bool hasRadiation = json.RootElement.TryGetProperty("current", out JsonElement current)
            && current.ValueKind == JsonValueKind.Object
            && current.TryGetProperty("shortwave_radiation", out JsonElement radiation)
            && radiation.ValueKind == JsonValueKind.Number;
        if (!hasRadiation) throw new InvalidDataException("weather response has no solar radiation");
        double lux = WeatherLuxPerWattPerSquareMeter * Math.Max(0, current.GetProperty("shortwave_radiation").GetDouble());
        lock (WeatherLock)
        {
            _lastWeatherLux = lux;
        }
        return lux;
    }
}
