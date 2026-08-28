using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit.SourceGenerators;

internal static class SyntaxExtensions
{
    public static NodeLocationInfo GetNodeLocationInfo(
        this SyntaxNode node)
    {
        var location = node.GetLocation();

        return new NodeLocationInfo(
            node.Span,
            location.GetLineSpan(),
            location.GetMappedLineSpan());
    }
}
