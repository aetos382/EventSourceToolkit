using System;
using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Aetos.EventSourceToolkit.SourceGenerators.Models;

namespace Aetos.EventSourceToolkit.SourceGenerators;

internal sealed class EventListenerParser
{
    private readonly SemanticModel _semanticModel;
    private readonly WellKnownSymbols _wellKnownSymbols;

    public EventListenerParser(
        SemanticModel semanticModel)
    {
        ArgumentNullException.ThrowIfNull(semanticModel);

        this._semanticModel = semanticModel;
        this._wellKnownSymbols = new WellKnownSymbols(semanticModel.Compilation);
    }

    public EventListenerInfoWithDiagnostics ParseEventListener(
        ClassDeclarationSyntax syntaxNode,
        INamedTypeSymbol symbol,
        ImmutableArray<AttributeData> attributes,
        CancellationToken cancellationToken)
    {
        var eventSourceName = this.GetEventSourceName(attributes);
        if (eventSourceName is null)
        {
            // TODO: diagnostic
            return EventListenerInfoWithDiagnostics.Empty;
        }

        foreach (var method in symbol.GetMethods())
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        return EventListenerInfoWithDiagnostics.Empty;
    }

    private string? GetEventSourceName(
        ImmutableArray<AttributeData> attributes)
    {
        var wellKnownSymbols = this._wellKnownSymbols;
        var comparer = SymbolEqualityComparer.Default;

        foreach (var attribute in attributes)
        {
            if (comparer.Equals(attribute.AttributeClass, wellKnownSymbols.GeneratedEventListenerAttribute))
            {
                return (string?)attribute.ConstructorArguments[0].Value;
            }
        }

        return null;
    }
}
