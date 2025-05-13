using Microsoft.Windows.ApplicationModel.Resources;

namespace AutoDarkModeLib.Helpers;

public static class ResourceExtensions
{
    private static readonly ResourceManager _resourceManager = new();
    private static ResourceContext _resourceContext = _resourceManager.CreateResourceContext();

    private static readonly ResourceMap _resourceMap =
        _resourceManager.MainResourceMap.TryGetSubtree("AutoDarkModeLib/Resources") ??
        _resourceManager.MainResourceMap.TryGetSubtree("Resources");

    public static string GetLocalized(this string resourceKey)
    {
        try
        {
            return _resourceMap?.GetValue(resourceKey, _resourceContext)?.ValueAsString ?? $"#{resourceKey}";
        }
        catch
        {
            return $"#{resourceKey}";
        }
    }

    public static void SetLanguage(string languageCode)
    {
        _resourceContext = _resourceManager.CreateResourceContext();
        _resourceContext.QualifierValues["Language"] = languageCode;
    }
}
