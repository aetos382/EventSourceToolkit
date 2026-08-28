using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Aetos.EventSourceToolkit.SourceGenerators;

internal sealed record NodeLocationInfo(
    TextSpan Span,
    FileLinePositionSpan LinePositionSpan,
    FileLinePositionSpan MappedLinePositionSpan)
{
    public Location CreateLocation()
    {
        var linePositionSpan = this.LinePositionSpan;
        var mappedLinePositionSpan = this.MappedLinePositionSpan;

        return Location.Create(
            linePositionSpan.Path,
            this.Span,
            linePositionSpan.Span,
            mappedLinePositionSpan.Path,
            mappedLinePositionSpan.Span);
    }
}
