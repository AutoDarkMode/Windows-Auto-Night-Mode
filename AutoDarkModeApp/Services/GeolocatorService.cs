using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutoDarkModeApp;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils;
using AutoDarkModeApp.ViewModels;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.IO;
using NetTopologySuite.IO.Converters;

public class GeolocatorService : IGeolocatorService
{
    private readonly STRtree<IFeature> _indexAdmin1 = new();
    private readonly STRtree<IFeature> _indexAdmin0 = new();
    private readonly string _langcode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToUpperInvariant();

    public GeolocatorService()
    {
        LoadGeoJsonIntoIndex(
            Path.Combine(AppContext.BaseDirectory, "Assets", "Geo", "ne_10m_admin_1_states_provinces.geojson"),
            _indexAdmin1);

        LoadGeoJsonIntoIndex(
            Path.Combine(AppContext.BaseDirectory, "Assets", "Geo", "ne_50m_admin_0_countries.geojson"),
            _indexAdmin0);

        _indexAdmin1.Build();
        _indexAdmin0.Build();

        var localSettings = App.GetService<ILocalSettingsService>();
        //TO-DO: Make async
        //TO-DO: use GetLanguageCodeAsync from LocalizationService
        //string? language = Task.Run(() => localSettings.ReadSettingAsync<string>("SelectedLanguageCode")).Result;
        string? language = Task.Run(LanguageConstants.GetDefaultLanguageAsync).Result;
        _langcode = CultureInfo.GetCultureInfo(language).TwoLetterISOLanguageName.ToUpperInvariant();
    }

    private void LoadGeoJsonIntoIndex(string jsonPath, STRtree<IFeature> index)
    {
        string json = File.ReadAllText(jsonPath);

        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new GeoJsonConverterFactory());

        var featureCollection = JsonSerializer.Deserialize<FeatureCollection>(json, opts);

        if (featureCollection == null) return;

        foreach (var feature in featureCollection)
        {
            if (feature.Geometry == null || feature.Geometry.IsEmpty)
                continue;

            index.Insert(feature.Geometry.EnvelopeInternal, feature);
        }
    }

    public Task<string?> GetRegionNameAsync(double longitude, double latitude)
    {
        var point = new Point(longitude, latitude);
        string cultureSpecificField = $"NAME_{_langcode}";

        string name = "Unknown";
        string? admin0name = FindNameAdmin0(point, _indexAdmin0, cultureSpecificField);
        if (admin0name != null)
            name = admin0name;

        string? admin1Name = FindNameAdmin1(point, _indexAdmin1);
        if (admin1Name != null)
            name += ", " + admin1Name;

        return Task.FromResult<string?>(name);
    }


    private static string? FindNameAdmin0(Point point, STRtree<IFeature> index, string cultureSpecificField)
    {
        var candidates = index.Query(point.EnvelopeInternal).OrderBy(f => f.Geometry.Distance(point));

        foreach (var feature in candidates)
        {
            if (feature.Geometry.Contains(point))
            {
                if (feature.Attributes.Exists(cultureSpecificField))
                    return feature.Attributes[cultureSpecificField]?.ToString();

                if (feature.Attributes.Exists("NAME_EN"))
                    return feature.Attributes["NAME_EN"]?.ToString();

                if (feature.Attributes.Exists("NAME"))
                    return feature.Attributes["NAME"]?.ToString();
            }
        }

        return null;
    }

    private static string? FindNameAdmin1(Point point, STRtree<IFeature> index)
    {
        var candidates = index.Query(point.EnvelopeInternal).OrderBy(f => f.Geometry.Distance(point));

        foreach (var feature in candidates)
        {
            if (feature.Geometry.Contains(point))
            {
                if (feature.Attributes.Exists("name"))
                    return feature.Attributes["name"]?.ToString();
            }
        }

        return null;
    }

}
