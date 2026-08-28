using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit.Analyzers.Properties;

internal partial class Resources
{
    public static LocalizableResourceString GetLocalizableResourceString(
        string name)
    {
        return new LocalizableResourceString(name, ResourceManager, typeof(Resources));
    }
}
