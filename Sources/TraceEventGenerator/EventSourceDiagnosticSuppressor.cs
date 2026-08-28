using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aetos.Tracing;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventSourceDiagnosticSuppressor :
    DiagnosticSuppressor
{
    /// <inheritdoc />
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
    }

    /// <inheritdoc />
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; }
}
