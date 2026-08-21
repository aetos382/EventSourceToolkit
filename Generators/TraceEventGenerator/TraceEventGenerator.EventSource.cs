
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.Tracing;

public partial class TraceEventGenerator
{
    private static EventSourceInfo ParseEventSourceClass(
        SemanticModel semanticModel,
        ClassDeclarationSyntax node,
        INamedTypeSymbol symbol,
        CancellationToken cancellationToken)
    {
        var parser = new EventSourceParser(semanticModel);

        return parser.ParseType(
            node,
            symbol,
            cancellationToken);
    }

    private static void EmitEventSourceClass(
        SourceProductionContext context,
        EventSourceInfo source)
    {
        foreach (var diagnostic in source.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.CreateDiagnostic());
        }
    }
}
