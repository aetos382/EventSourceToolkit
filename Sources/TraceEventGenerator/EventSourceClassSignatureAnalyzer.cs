using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Aetos.Tracing.Diagnostics;
using Aetos.Tracing.Properties;

namespace Aetos.Tracing;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventSourceClassSignatureAnalyzer :
    DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor EventSourceClassMustEventSourceAttribute = new(
        DiagnosticIds.EventSourceClassMustHaveEventSourceAttribute,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustHaveEventSourceAttributeTitle)),
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustHaveEventSourceAttributeMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustHaveEventSourceAttributeDescription)));

    private static readonly DiagnosticDescriptor EventSourceClassMustNotBeAbstract = new(
        DiagnosticIds.EventSourceClassMustNotBeAbstract,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeAbstractTitle)),
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeAbstractMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeAbstractDescription)));

    /// <inheritdoc />
    public override void Initialize(
        AnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.ClassDeclaration);
    }

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        EventSourceClassMustEventSourceAttribute,
        EventSourceClassMustNotBeAbstract
    ];

    private static void SyntaxNodeAction(
        SyntaxNodeAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var compilation = context.Compilation;
        var semanticModel = context.SemanticModel;
        var wellKnownTypes = new WellKnownTypeSymbols(compilation);

        var node = (ClassDeclarationSyntax)context.Node;
        var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

        if (symbol is null)
        {
            return;
        }

        if (!symbol.HasAttribute(wellKnownTypes.GeneratedEventSourceAttribute))
        {
            return;
        }

        if (symbol.IsAbstract)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                EventSourceClassMustNotBeAbstract,
                node.GetLocation(),
                symbol.Name));

            return;
        }

        var eventSourceAttribute = symbol.GetAttribute(wellKnownTypes.EventSourceAttribute);
        if (eventSourceAttribute is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                EventSourceClassMustEventSourceAttribute,
                node.GetLocation(),
                symbol.Name));

            return;
        }
    }
}
