using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit.SourceGenerators.Properties;

internal partial class Resources
{
    public static LocalizableResourceString GetLocalizableResourceString(
        string name)
    {
        return new LocalizableResourceString(name, ResourceManager, typeof(Resources));
    }
}
