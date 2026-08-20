using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Aetos.Tracing;

internal sealed class EventSourceParser
{
    private readonly SemanticModel _semanticModel;
    private readonly WellKnownTypeSymbols _wellKnownTypes;

    public EventSourceParser(
        SemanticModel semanticModel)
    {
        ArgumentNullException.ThrowIfNull(semanticModel);

        this._semanticModel = semanticModel;
        this._wellKnownTypes = new WellKnownTypeSymbols(semanticModel.Compilation);
    }

    public EventSourceInfo ParseType(
        ClassDeclarationSyntax node,
        INamedTypeSymbol symbol,
        CancellationToken cancellationToken)
    {
        if (!IsValidClassModifiers(node))
        {
            var result = new EventSourceInfo
            {
                DiagnosticInfo = new(
                    DiagnosticIds.EventSourceClassMustHaveValidSignature,
                    node.CreateLocationInfo())
            };

            return result;
        }

        var eventSourceName = this.GetEventSourceName(symbol);
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

        if (!this.IsDerivedFromEventSource(symbol))
        {
            var result = new EventSourceInfo
            {
                DiagnosticInfo = new(
                    DiagnosticIds.EventSourceClassMustInheritFromEventSource,
                    node.CreateLocationInfo())
            };

            return result;
        }

        var semanticModel = this._semanticModel;
        var wellKnownTypes = this._wellKnownTypes;

        foreach (var method in node.GetMethods())
        {
            var returnsVoid = method.ReturnsVoid;

            if (!method.HasPartialModifier)
            {
                continue;
            }

            var methodSymbol = semanticModel.GetDeclaredSymbol(method, cancellationToken)!;

            if (methodSymbol.HasAttribute(wellKnownTypes.NonEventAttribute))
            {
                continue;
            }

            var eventAttribute = methodSymbol.GetAttribute(wellKnownTypes.EventAttribute);
        }

        return new();
    }

    private static bool IsValidClassModifiers(
        ClassDeclarationSyntax node)
    {
        return node is { HasPartialModifier: true, HasFileModifier: false };
    }

    private string? GetEventSourceName(INamedTypeSymbol type)
    {
        var wellKnownTypes = this._wellKnownTypes;

        var attribute = type.GetAttribute(wellKnownTypes.EventSourceAttribute);
        if (attribute is null)
        {
            return null;
        }

        foreach (var (name, value) in attribute.NamedArguments)
        {
            if (name != nameof(EventSourceAttribute.Name))
            {
                continue;
            }

            if (!SymbolEqualityComparer.Default.Equals(value.Type, wellKnownTypes.String))
            {
                continue;
            }

            return (string?)value.Value;
        }

        return null;
    }

    private bool IsDerivedFromEventSource(INamedTypeSymbol type)
    {
        return type.IsDerivedFrom(this._wellKnownTypes.EventSource);
    }
}
