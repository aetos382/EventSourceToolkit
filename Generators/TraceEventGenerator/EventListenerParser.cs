using System;
using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Aetos.Tracing.Models;

namespace Aetos.Tracing;

internal sealed class EventListenerParser
{
    private readonly SemanticModel _semanticModel;
    private readonly WellKnownTypeSymbols _wellKnownTypes;

    public EventListenerParser(
        SemanticModel semanticModel)
    {
        ArgumentNullException.ThrowIfNull(semanticModel);

        this._semanticModel = semanticModel;
        this._wellKnownTypes = new WellKnownTypeSymbols(semanticModel.Compilation);
    }

    public EventListenerInfoWithDiagnostics ParseEventListener(
        ClassDeclarationSyntax syntaxNode,
        INamedTypeSymbol symbol,
        ImmutableArray<AttributeData> attributes,
        CancellationToken cancellationToken)
    {
        var wellKnownTypes = this._wellKnownTypes;

        var eventSourceName = this.GetEventSourceName(attributes);
        if (eventSourceName is null)
        {
            // TODO: diagnostic
            return EventListenerInfoWithDiagnostics.Empty;
        }

        if (!symbol.IsDerivedFrom(wellKnownTypes.EventListener))
        {
            // TODO: diagnostic
            return EventListenerInfoWithDiagnostics.Empty;
        }

        return EventListenerInfoWithDiagnostics.Empty;
    }

    private string? GetEventSourceName(
        ImmutableArray<AttributeData> attributes)
    {
        var wellKnownTypes = this._wellKnownTypes;
        var comparer = SymbolEqualityComparer.Default;

        foreach (var attribute in attributes)
        {
            if (comparer.Equals(attribute.AttributeClass, wellKnownTypes.GeneratedEventListenerAttribute))
            {
                return (string?)attribute.ConstructorArguments[0].Value;
            }
        }

        return null;
    }
}
