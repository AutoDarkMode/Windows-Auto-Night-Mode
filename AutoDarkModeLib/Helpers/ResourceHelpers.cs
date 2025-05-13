using Microsoft.UI.Xaml.Markup;
using Microsoft.Windows.ApplicationModel.Resources;

namespace AutoDarkModeLib.Helpers;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed partial class ResourceString : MarkupExtension
{
    private static readonly ResourceManager resourceManager = new();
    private static readonly ResourceContext resourceContext = resourceManager.CreateResourceContext();
    private static readonly ResourceMap resourceMap = resourceManager.MainResourceMap.GetSubtree("AutoDarkModeLib/Resources");

    public string Name { get; set; } = string.Empty;

    protected override object ProvideValue()
    {
        try
        {
            return resourceMap?.GetValue(Name, resourceContext)?.ValueAsString ?? $"#{Name}";
        }
        catch
        {
            return $"#{Name}";
        }
    }
}