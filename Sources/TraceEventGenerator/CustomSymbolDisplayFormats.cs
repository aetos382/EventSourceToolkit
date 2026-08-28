using Microsoft.CodeAnalysis;

namespace Aetos.Tracing;

internal static class CustomSymbolDisplayFormats
{
    public static readonly SymbolDisplayFormat FullyQualifiedFormat = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);
}
