
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
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
        var compilation = context.SemanticModel.Compilation;
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

        if (!IsDerivedFromEventSource(compilation, symbol))
        {
            var result = new EventSourceInfo
            {
                DiagnosticInfo = new(
                    DiagnosticIds.EventSourceClassMustInheritFromEventSource,
                    node.CreateLocationInfo())
            };

            return result;
        }

        var eventAttributeSymbol = compilation.GetTypeByMetadataName("System.Diagnostics.Tracing.EventAttribute");
        var nonEventAttributeSymbol = compilation.GetTypeByMetadataName("System.Diagnostics.Tracing.NonEventAttribute");

        foreach (var member in symbol.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            var attributes = method.GetAttributes();

            if (attributes.Any(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, nonEventAttributeSymbol)))
            {
                continue;
            }

            var eventAttribute = attributes.SingleOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, eventAttributeSymbol));

            var syntaxRefs = method.DeclaringSyntaxReferences;

            foreach (var syntaxRef in syntaxRefs)
            {
                var syntax = (MethodDeclarationSyntax)syntaxRef.GetSyntax(cancellationToken);
            }
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

    private static bool IsDerivedFromEventSource(
        Compilation compilation,
        INamedTypeSymbol myEventSourceType)
    {
        var eventSourceType = compilation.GetTypeByMetadataName("System.Diagnostics.Tracing.EventSource");

        var currentType = myEventSourceType.BaseType;

        while (currentType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentType, eventSourceType))
            {
                return true;
            }

            currentType = currentType.BaseType;
        }

        return false;
    }
}
