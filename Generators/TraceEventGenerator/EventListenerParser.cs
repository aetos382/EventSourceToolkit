using System;
using System.Threading;

using Aetos.Tracing.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    public EventListenerInfo ParseEventListener(
        ClassDeclarationSyntax syntaxNode,
        INamedTypeSymbol symbol,
        CancellationToken cancellationToken)
    {
        return new();
    }
}
