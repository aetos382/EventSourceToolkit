using Microsoft.CodeAnalysis;

namespace Aetos.Tracing;

internal static class CustomSymbolDisplayFormats
{
    public static readonly SymbolDisplayFormat FullyQualifiedTypeFormat = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
}
