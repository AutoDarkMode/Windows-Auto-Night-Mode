using System;
using System.Runtime.InteropServices;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using Microsoft.Windows.ApplicationModel.Resources;

namespace AutoDarkModeLib.Helpers;

public static class ResourceExtensions
{
    private static readonly ResourceManager _resourceManager = new();
    private static ResourceContext _resourceContext = _resourceManager.CreateResourceContext();

    private static readonly ResourceMap _resourceMap =
        _resourceManager.MainResourceMap.TryGetSubtree("AutoDarkModeLib/Resources")
        ?? _resourceManager.MainResourceMap.TryGetSubtree("Resources")
        ?? throw new InvalidOperationException("Can't find resources");

    public static string GetLocalized(this string resourceKey)
    {
        if (_resourceMap == null)
            return $"#MISSING_MAP:{resourceKey}";

        try
        {
            System.Diagnostics.Debug.WriteLine(resourceKey);
            var resourceCandidate = _resourceMap.GetValue(resourceKey, _resourceContext);
            return resourceCandidate?.ValueAsString ?? $"#MISSING:{resourceKey}";
        }
        catch (COMException ex)
        {
            return $"#NOT_FOUND:{resourceKey}({ex.Message})";
        }
        catch (Exception ex)
        {
            return $"#ERROR:{resourceKey}({ex.Message})";
        }
    }

    public static void SetLanguage(string languageCode)
    {
        Bootstrap.Initialize(0x00010007);
        _resourceContext = _resourceManager.CreateResourceContext();
        _resourceContext.QualifierValues["Language"] = languageCode;
    }
}
