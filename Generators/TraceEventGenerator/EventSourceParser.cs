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
        var diagnostics = new List<DiagnosticInfo>();

        if (!IsValidClassModifiers(node))
        {
            diagnostics.Add(
                new(
                    DiagnosticIds.EventSourceClassMustHaveValidSignature, node.CreateLocationInfo()));
        }

        var eventSourceName = this.GetEventSourceName(symbol);
        if (eventSourceName is null)
        {
            diagnostics.Add(
                new(
                    DiagnosticIds.EventSourceClassMustHaveValidEventSourceAttribute, node.CreateLocationInfo()));
        }

        if (!this.IsDerivedFromEventSource(symbol))
        {
            diagnostics.Add(
                new(
                    DiagnosticIds.EventSourceClassMustInheritFromEventSource, node.CreateLocationInfo()));
        }

        var semanticModel = this._semanticModel;
        var wellKnownTypes = this._wellKnownTypes;

        foreach (var method in node.GetMethods())
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(method, cancellationToken)!;
            var eventAttribute = methodSymbol.GetAttribute(wellKnownTypes.EventAttribute);
            var hasEventAttribute = eventAttribute is not null;

            // [NonEvent] が付いているメソッドは無視
            if (methodSymbol.HasAttribute(wellKnownTypes.NonEventAttribute))
            {
                if (hasEventAttribute)
                {
                    // [Event] と [NonEvent] が両方ついていたら警告
                    diagnostics.Add(
                        new(DiagnosticIds.EventSourceMethodMustHaveValidAttributes, method.CreateLocationInfo()));
                }

                continue;
            }

            if (hasEventAttribute)
            {
                // static だったらエラー
                if (method.IsStatic)
                {
                    diagnostics.Add(new(
                        DiagnosticIds.EventSourceMethodMustHaveValidSignature,
                        method.CreateLocationInfo()));
                }
            }

            var returnsVoid = method.ReturnsVoid;
            if (!returnsVoid || !method.HasPartialModifier)
            {
                diagnostics.Add(new(
                    DiagnosticIds.EventSourceMethodMustHaveValidSignature,
                    method.CreateLocationInfo()));
            }
        }

        return new(diagnostics.ToArray());
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
