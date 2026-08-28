namespace Aetos.EventSourceToolkit;

internal static class Constants
{
    public const string Namespace = "Aetos.EventSourceToolkit";

    public const string GeneratedFileExtension = "g.cs";

    public const string GeneratedEventSourceAttributeName = "GeneratedEventSourceAttribute";

    public const string GeneratedEventListenerAttributeName = "GeneratedEventListenerAttribute";

    public const string GeneratedEventSourceAttributeFullName = $"{Namespace}.{GeneratedEventSourceAttributeName}";

    public const string GeneratedEventListenerAttributeFullName = $"{Namespace}.{GeneratedEventListenerAttributeName}";

    public const string GeneratedEventSourceAttributeFileName = $"{GeneratedEventSourceAttributeFullName}.{GeneratedFileExtension}";

    public const string GeneratedEventListenerAttributeFileName = $"{GeneratedEventListenerAttributeFullName}.{GeneratedFileExtension}";
}
