using Microsoft.CodeAnalysis;

namespace Aetos.Tracing;

internal static class CustomSymbolDisplayFormats
{
    public static readonly SymbolDisplayFormat FullyQualifiedFormatWithoutGlobalPrefix = new(
        SymbolDisplayGlobalNamespaceStyle.Omitted,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
}
