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
using AutoDarkModeLib.Configs;
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
    // lux per W/m^2 of global radiation, measurements range from 100 to 115
    private const double LuminousEfficacyLmPerW = 110;
    // what reaches the sensor of a room near a window compared to outdoors, the scale the thresholds are given in
    private const double WindowDaylightFactor = 0.01;
    // visibility of a clean atmosphere, beyond it the haze is not told apart
    private const double CleanVisibilityKm = 25;
    // the aerosols sit in roughly the lowest 1.2 km
    private const double AerosolDepthPerKmExtinction = 1.2;
    private const double AutomaticDarkOutdoorLux = 4000;
    private const double AutomaticLightOutdoorLux = 6000;
    // the mean luminance needs no detail, a small frame keeps the per-frame conversion and copy cheap
    private static readonly BitmapSize WebcamFrameSize = new() { Width = 160, Height = 120 };

    // the key and app id are the public ones of the Windows Weather app
    private const string BingWeatherUrl = "https://api.msn.com/weather/current?locale=en-US&lat={0}&lon={1}&units=C&appId=9e21380c-ff19-4c78-b4ea-19558e93a5d3&apiKey=OkWqHMuutahBXs3dBoygqCjgXRt6CV4i5V7SRQURrT&ocid=weather-app-win&wrapOData=false";

    private static readonly HttpClient WeatherClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly object WeatherLock = new();
    private static DateTime _lastWeatherRequest;
    private static (double Lat, double Lon, WeatherProvider Provider) _lastWeatherRequestKey;
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
    /// Returns the light level from the current sunshine at the location reported by the weather provider.
    /// The provider is asked at most every <see cref="MinimumWeatherIntervalMin"/> minutes, failed requests included.
    /// </summary>
    public static async Task<double> FetchWeatherLuxAsync(double lat, double lon, WeatherProvider provider)
    {
        bool isValidLocation = Math.Abs(lat) <= 90 && Math.Abs(lon) <= 180;
        if (!isValidLocation) throw new ArgumentOutOfRangeException(nameof(lat), $"invalid location {lat}, {lon}");

        lock (WeatherLock)
        {
            bool isRecentRequestForLocation = _lastWeatherRequestKey == (lat, lon, provider) && DateTime.Now - _lastWeatherRequest < TimeSpan.FromMinutes(MinimumWeatherIntervalMin);
            if (isRecentRequestForLocation)
            {
                return _lastWeatherLux ?? throw new InvalidOperationException("last weather request failed");
            }
            _lastWeatherRequest = DateTime.Now;
            _lastWeatherRequestKey = (lat, lon, provider);
            _lastWeatherLux = null;
        }

        double lux = provider switch
        {
            WeatherProvider.Bing => await FetchBingLuxAsync(lat, lon),
            _ => WeatherLuxPerWattPerSquareMeter * Math.Max(0, await FetchOpenMeteoRadiationAsync(lat, lon))
        };
        lock (WeatherLock)
        {
            _lastWeatherLux = lux;
        }
        return lux;
    }

    private static async Task<double> FetchOpenMeteoRadiationAsync(double lat, double lon)
    {
        string url = string.Format(CultureInfo.InvariantCulture, "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=shortwave_radiation", lat, lon);
        string data = await WeatherClient.GetStringAsync(url);
        using JsonDocument json = JsonDocument.Parse(data);
        bool hasRadiation = json.RootElement.TryGetProperty("current", out JsonElement current)
            && current.ValueKind == JsonValueKind.Object
            && current.TryGetProperty("shortwave_radiation", out JsonElement radiation)
            && radiation.ValueKind == JsonValueKind.Number;
        if (!hasRadiation) throw new InvalidDataException("weather response has no solar radiation");
        return current.GetProperty("shortwave_radiation").GetDouble();
    }

    // Bing Weather reports no radiation, so the light level is modelled from the sun height and its sky, haze and UV readings
    private static async Task<double> FetchBingLuxAsync(double lat, double lon)
    {
        string url = string.Format(CultureInfo.InvariantCulture, BingWeatherUrl, lat, lon);
        string data = await WeatherClient.GetStringAsync(url);
        using JsonDocument json = JsonDocument.Parse(data);
        JsonElement current = json.RootElement.GetProperty("responses")[0].GetProperty("weather")[0].GetProperty("current");
        bool hasCloudCover = current.TryGetProperty("cloudCover", out JsonElement cloudCover) && cloudCover.ValueKind == JsonValueKind.Number;
        if (!hasCloudCover) throw new InvalidDataException("weather response has no cloud cover");

        double? visibilityKm = TryGetNumber(current, "vis");
        double? uvIndex = TryGetNumber(current, "uv");
        double sunHeight = GetSunHeightRad(lat, lon, DateTime.UtcNow);
        return WindowDaylightFactor * EstimateOutdoorIlluminance(sunHeight, cloudCover.GetDouble() / 100, visibilityKm, uvIndex);
    }

    private static double? TryGetNumber(JsonElement element, string name)
    {
        bool hasNumber = element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number;
        return hasNumber ? value.GetDouble() : null;
    }

    /// <summary>
    /// Models the illuminance in lux on an unshaded horizontal surface outdoors.
    /// Clear sky: Haurwitz global irradiance, which already includes the air mass.
    /// Haze: the extinction of the visibility (Koschmieder) beyond a clean atmosphere weakens the sunshine.
    /// Clouds: Kasten and Czeplak, G = Gclear * (1 - 0.75 * N^3.4), blended with the cloudiness the UV index implies once the sun is high enough to read it.
    /// Twilight: the sun below the horizon still lights the sky, interpolated in log steps between the known levels.
    /// </summary>
    public static double EstimateOutdoorIlluminance(double sunHeightRad, double cloudFraction, double? visibilityKm, double? uvIndex)
    {
        double clouds = Math.Clamp(cloudFraction, 0, 1);
        double cloudTransmission = 1 - 0.75 * Math.Pow(clouds, 3.4);

        double sinHeight = Math.Sin(sunHeightRad);
        double sunlight = 0;
        if (sinHeight > 0)
        {
            double clearSky = 1098 * sinHeight * Math.Exp(-0.059 / sinHeight);

            // aerosol optical depth is about the extinction coefficient times the aerosol layer height, the global radiation only loses about half of what the direct beam loses
            double airMass = 1 / (sinHeight + 0.50572 * Math.Pow(6.07995 + sunHeightRad * 180 / Math.PI, -1.6364));
            double extraDepth = Math.Max(0, AerosolDepthPerKmExtinction * (3.912 / Math.Clamp(visibilityKm ?? CleanVisibilityKm, 0.05, CleanVisibilityKm) - 3.912 / CleanVisibilityKm));
            double hazeTransmission = Math.Exp(-0.5 * extraDepth * airMass);

            // UV index of a clear sky: 12.5 * cos(zenith)^2.42, Bing rounds it, so it is only trusted when the clear sky value is large
            double clearSkyUv = 12.5 * Math.Pow(sinHeight, 2.42);
            bool isUvReadable = uvIndex.HasValue && clearSkyUv >= 6;
            double blendedCloudTransmission = isUvReadable
                ? 0.5 * cloudTransmission + 0.5 * Math.Clamp(uvIndex.Value / clearSkyUv, 0, 1)
                : cloudTransmission;

            sunlight = LuminousEfficacyLmPerW * clearSky * hazeTransmission * blendedCloudTransmission;
        }

        // sunrise and sunset 400 lux, end of civil twilight 3.4 lux, nautical 0.008 lux, astronomical 0.0001 lux
        double sunHeightDeg = sunHeightRad * 180 / Math.PI;
        double twilight = sunHeightDeg switch
        {
            >= 0 => 400,
            >= -6 => Math.Pow(10, Math.Log10(3.4) + (Math.Log10(400) - Math.Log10(3.4)) * (sunHeightDeg + 6) / 6),
            >= -12 => Math.Pow(10, Math.Log10(0.008) + (Math.Log10(3.4) - Math.Log10(0.008)) * (sunHeightDeg + 12) / 6),
            >= -18 => Math.Pow(10, Math.Log10(0.0001) + (Math.Log10(0.008) - Math.Log10(0.0001)) * (sunHeightDeg + 18) / 6),
            _ => 0.0001
        };
        return Math.Max(sunlight, twilight * cloudTransmission);
    }

    /// <summary>
    /// Whether the switch points come from the light source itself instead of the user thresholds
    /// </summary>
    public static bool UsesAutomaticThresholds(AmbientLightSource source, WeatherProvider provider)
        => source == AmbientLightSource.Weather && provider == WeatherProvider.Bing;

    /// <summary>
    /// Returns the lux values below which it is dark and above which it is light. The modelled weather light needs no tuning, so it
    /// switches around the 5000 lux outdoor level of the standard overcast design sky, where a room with a 2% daylight factor drops to 100 lux
    /// and needs artificial light. The gap between the two values keeps a drifting reading from flapping the theme.
    /// </summary>
    public static (double Dark, double Light) GetThresholds(AmbientLight config)
    {
        return UsesAutomaticThresholds(config.Source, config.WeatherProvider)
            ? (AutomaticDarkOutdoorLux * WindowDaylightFactor, AutomaticLightOutdoorLux * WindowDaylightFactor)
            : (config.DarkThreshold, config.LightThreshold);
    }

    /// <summary>
    /// Returns the height of the sun above the horizon in radians (approximate solar position algorithm)
    /// </summary>
    public static double GetSunHeightRad(double lat, double lon, DateTime utc)
    {
        double dayOfYear = utc.DayOfYear + (utc.Hour + utc.Minute / 60.0) / 24;
        double gamma = 2 * Math.PI / 365 * (dayOfYear - 1);
        double declination = 0.006918 - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma)
            - 0.006758 * Math.Cos(2 * gamma) + 0.000907 * Math.Sin(2 * gamma);
        double equationOfTimeMin = 229.18 * (0.000075 + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma)
            - 0.014615 * Math.Cos(2 * gamma) - 0.040849 * Math.Sin(2 * gamma));
        double solarTimeMin = utc.Hour * 60 + utc.Minute + utc.Second / 60.0 + equationOfTimeMin + 4 * lon;
        double hourAngle = (solarTimeMin / 4 - 180) * Math.PI / 180;
        double latRad = lat * Math.PI / 180;
        double sinHeight = Math.Sin(latRad) * Math.Sin(declination) + Math.Cos(latRad) * Math.Cos(declination) * Math.Cos(hourAngle);
        return Math.Asin(Math.Clamp(sinHeight, -1, 1));
    }
}
