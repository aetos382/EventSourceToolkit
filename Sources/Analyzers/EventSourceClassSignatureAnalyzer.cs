using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Aetos.EventSourceToolkit.Analyzers.Properties;

namespace Aetos.EventSourceToolkit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventSourceClassSignatureAnalyzer :
    DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor EventSourceClassMustNotBeAbstract = new(
        DiagnosticIds.EventSourceClassMustNotBeAbstract,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeAbstractTitle)),
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeAbstractMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeAbstractDescription)));
    private static readonly DiagnosticDescriptor EventSourceClassMustBeInheritFromEventSource = new(
        DiagnosticIds.EventSourceClassMustBeInheritFromEventSource,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustBeInheritFromEventSourceTitle)),
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustBeInheritFromEventSourceMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustBeInheritFromEventSourceDescription)));

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
        EventSourceClassMustNotBeAbstract,
        EventSourceClassMustBeInheritFromEventSource
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

        var symbolFullName = symbol.ToDisplayString(CustomSymbolDisplayFormats.FullyQualifiedFormatWithoutGlobalPrefix);
        var abstractModifierOrNull = node.Modifiers.FirstOrNull(static x => x.IsKind(SyntaxKind.AbstractKeyword));

        if (abstractModifierOrNull is {} abstractModifier)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                EventSourceClassMustNotBeAbstract,
                abstractModifier.GetLocation(),
                symbolFullName));

            return;
        }

        if (!symbol.IsDerivedFrom(wellKnownTypes.EventSource))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                EventSourceClassMustBeInheritFromEventSource,
                node.Identifier.GetLocation(),
                symbolFullName));

            return;
        }
    }
}
