
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
        var node = (ClassDeclarationSyntax)context.TargetNode;
        var symbol = (INamedTypeSymbol)context.TargetSymbol;

        if (!node.Modifiers.Any(SyntaxKind.PartialKeyword) || node.Modifiers.Any(SyntaxKind.FileKeyword))
        {
            var result = new EventSourceInfo
            {
                DiagnosticInfo = new(
                    DiagnosticIds.EventSourceClassMustHaveValidSignature,
                    node.CreateLocationInfo())
            };

            return result;
        }

        var eventSourceName = GetEventSourceName(context.SemanticModel.Compilation, symbol);
        if (eventSourceName is null)
        {
            var result = new EventSourceInfo
            {
                DiagnosticInfo = new(
                    DiagnosticIds.EventSourceClassMustHaveValidEventSourceAttribute,
                    node.CreateLocationInfo())
            };

            return result;
        }

        return new();
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

    private static string? GetEventSourceName(
        Compilation compilation,
        INamedTypeSymbol typeSymbol)
    {
        var eventSourceAttributeSymbol =
            compilation.GetTypeByMetadataName("System.Diagnostics.Tracing.EventSourceAttribute");

        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        if (eventSourceAttributeSymbol is null)
        {
            return null;
        }

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, eventSourceAttributeSymbol))
            {
                continue;
            }

            foreach (var (name, value) in attribute.NamedArguments)
            {
                if (name != nameof(EventSourceAttribute.Name))
                {
                    continue;
                }

                if (!SymbolEqualityComparer.Default.Equals(value.Type, stringType))
                {
                    continue;
                }

                return (string?)value.Value;
            }
        }

        return null;
    }
}
