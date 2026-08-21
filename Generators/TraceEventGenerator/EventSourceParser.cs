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

        var eventMethods = new List<EventSourceMethodInfo>();

        foreach (var method in node.GetMethods())
        {
            /*
             * 本来あるべきでない（イベント記録メソッドとして不適切な形式の）メソッドに [Event] がついていたら TEG004
             * - 戻り値が void でない
             * - static である
             * - file である
             * - partial がない
             */

            /*
             * EventSource は [Event] がなくてもイベント記録メソッドとして扱うが、本ジェネレータは [Event] が付いているものをだけを扱う。
             * 適切な形式のメソッドに [Event] が付いていない場合は付けることを推奨する警告 TEG006 を上げる
             */

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

            var validSignature = true;

            if (hasEventAttribute)
            {
                // static だったらエラー
                if (method.IsStatic)
                {
                    diagnostics.Add(new(
                        DiagnosticIds.EventSourceMethodMustHaveValidSignature,
                        method.CreateLocationInfo()));

                    validSignature = false;
                }
            }

            var returnsVoid = method.ReturnsVoid;
            if (!returnsVoid || !method.HasPartialModifier)
            {
                diagnostics.Add(new(
                    DiagnosticIds.EventSourceMethodMustHaveValidSignature,
                    method.CreateLocationInfo()));

                validSignature = false;
            }

            if (validSignature && !hasEventAttribute)
            {
                diagnostics.Add(new(
                    DiagnosticIds.EventSourceMethodShouldHaveEventAttribute,
                    method.CreateLocationInfo()));
            }

            if (!validSignature)
            {
                continue;
            }

            var parameters = new List<EventSourceMethodParameterInfo>();

            foreach (var parameter in method.ParameterList.Parameters)
            {
                var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken)!;
                var parameterType = parameterSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                parameters.Add(new(parameterType, parameter.Identifier.Text));
            }

            eventMethods.Add(new(method.Identifier.Text, parameters.ToArray()));
        }

        EventSourceClassInfo? classInfo = null;

        if (eventSourceName is not null)
        {
            var ancestors = new List<AncestorTypeInfo>();
            var parent = node.Parent;

            while (parent is not null and not CompilationUnitSyntax)
            {
                var syntaxKind = parent.Kind();

                if (syntaxKind is (SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration))
                {
                    break;
                }

                if (parent is not TypeDeclarationSyntax typeNode)
                {
                    break;
                }

                var typeKind = syntaxKind switch
                {
                    SyntaxKind.ClassDeclaration => TypeKind.Class,
                    SyntaxKind.StructDeclaration => TypeKind.Struct,
                    SyntaxKind.InterfaceDeclaration => TypeKind.Interface,
                    SyntaxKind.RecordDeclaration => TypeKind.Record
                };

                ancestors.Insert(0, new(typeKind, typeNode.Identifier.Text));

                parent = parent.Parent;
            }

            string? namespaceName = null;

            if (parent is BaseNamespaceDeclarationSyntax namespaceNode)
            {
                var namespaceSymbol = semanticModel.GetDeclaredSymbol(namespaceNode)!;
                namespaceName = namespaceSymbol.ToDisplayString(CustomSymbolDisplayFormats.FullyQualifiedFormatWithoutGlobalPrefix);
            }

            classInfo = new(namespaceName, ancestors.ToArray(), node.Identifier.Text, eventSourceName);
        }

        return new(classInfo, eventMethods.ToArray(), diagnostics.ToArray());
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
