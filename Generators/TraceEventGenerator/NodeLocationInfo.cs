using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Aetos.Tracing;

internal sealed record NodeLocationInfo(
    FileLinePositionSpan LinePositionSpan,
    TextSpan Span)
{
    public Location CreateLocation()
    {
        return Location.Create(
            this.LinePositionSpan.Path,
            this.Span,
            this.LinePositionSpan.Span);
    }
}
