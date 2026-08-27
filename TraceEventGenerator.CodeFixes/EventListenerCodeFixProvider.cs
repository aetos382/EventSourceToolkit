using System.Collections.Immutable;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CodeFixes;

namespace TraceEventGenerator.CodeFixes;

public sealed class EventListenerCodeFixProvider :
    CodeFixProvider
{
    /// <inheritdoc />
    public override Task RegisterCodeFixesAsync(
        CodeFixContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
}
