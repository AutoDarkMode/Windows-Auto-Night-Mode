using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Markup;
using Microsoft.Windows.ApplicationModel.Resources;

namespace AutoDarkModeLib.Helpers;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed partial class ResourceString : MarkupExtension
{
    private static readonly ResourceManager _resourceManager = new();
    private static readonly ResourceContext _resourceContext = _resourceManager.CreateResourceContext();
    private static readonly ResourceMap _resourceMap = _resourceManager.MainResourceMap.GetSubtree("AutoDarkModeLib/Resources");

    public string Name { get; set; } = string.Empty;

    protected override object ProvideValue()
    {
        if (_resourceMap == null)
            return $"#MISSING_MAP:{Name}";

        try
        {
            return _resourceMap?.GetValue(Name, _resourceContext)?.ValueAsString ?? $"#MISSING:{Name}";
        }
        catch (COMException ex)
        {
            return $"#NOT_FOUND:{Name}({ex.Message})";
        }
        catch (Exception ex)
        {
            return $"#ERROR:{Name}({ex.Message})";
        }
    }
}
