using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit;

internal static class CustomSymbolDisplayFormats
{
    public static readonly SymbolDisplayFormat FullyQualifiedFormat = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);
}
