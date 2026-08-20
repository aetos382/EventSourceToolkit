
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.Tracing;

public partial class TraceEventGenerator
{
    private static EventSourceInfo ParseEventSourceClass(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var parser = new EventSourceParser(context.SemanticModel);

        return parser.ParseType(
            (ClassDeclarationSyntax)context.TargetNode,
            (INamedTypeSymbol)context.TargetSymbol,
            cancellationToken);
    }

    private static void EmitEventSourceClass(
        SourceProductionContext context,
        EventSourceInfo source)
    {
        if (source.DiagnosticInfo is { } diagnosticInfo)
        {
            context.ReportDiagnostic(diagnosticInfo.CreateDiagnostic());
        }
    }
}
