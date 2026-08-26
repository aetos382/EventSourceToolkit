namespace Aetos.Tracing;

internal static class Constants
{
    public const string Namespace = "Aetos.Tracing";

    public const string GeneratedFileExtension = "g.cs";

    public const string GeneratedEventSourceAttributeName = "GeneratedEventSourceAttribute";

    public const string GeneratedEventListenerAttributeName = "GeneratedEventListenerAttribute";

    public const string GeneratedEventListenerMarkerAttributeName = "GeneratedEventListenerMarkerAttribute";

    public const string GeneratedEventAttributeName = "GeneratedEventAttribute";

    public const string GeneratedEventSourceAttributeFullName = $"{Namespace}.{GeneratedEventSourceAttributeName}";

    public const string GeneratedEventListenerAttributeFullName = $"{Namespace}.{GeneratedEventListenerAttributeName}";

    public const string GeneratedEventListenerMarkerAttributeFullName = $"{Namespace}.{GeneratedEventListenerMarkerAttributeName}";

    public const string GeneratedEventAttributeFullName = $"{Namespace}.{GeneratedEventAttributeName}";

    public const string GeneratedEventSourceAttributeFileName = $"{GeneratedEventSourceAttributeFullName}.{GeneratedFileExtension}";

    public const string GeneratedEventListenerAttributeFileName = $"{GeneratedEventListenerAttributeFullName}.{GeneratedFileExtension}";

    public const string GeneratedEventListenerMarkerAttributeFileName = $"{GeneratedEventListenerMarkerAttributeFullName}.{GeneratedFileExtension}";

    public const string GeneratedEventAttributeFileName = $"{GeneratedEventAttributeFullName}.{GeneratedFileExtension}";
}
