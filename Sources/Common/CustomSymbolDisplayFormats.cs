using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit;

public static class CustomSymbolDisplayFormats
{
    public static readonly SymbolDisplayFormat FullyQualifiedFormat = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

    public static readonly SymbolDisplayFormat FullyQualifiedFormatWithoutGlobalPrefix = new(
        SymbolDisplayGlobalNamespaceStyle.Omitted,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);
}
