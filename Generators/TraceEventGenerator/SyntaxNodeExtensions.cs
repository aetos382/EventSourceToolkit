using Microsoft.CodeAnalysis;

namespace Aetos.Tracing;

internal static class SyntaxNodeExtensions
{
    extension(SyntaxNode node)
    {
        public NodeLocationInfo CreateLocationInfo()
        {
            return new NodeLocationInfo(node.GetLocation().GetLineSpan(), node.Span);
        }
    }
}
