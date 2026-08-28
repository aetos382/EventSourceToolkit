using System.Collections.Generic;
using System.Collections.Immutable;

using Aetos.EventSourceToolkit.Analyzers.Properties;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aetos.EventSourceToolkit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventSourceNestedTypeVisibilitySuppressor :
    DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor EventSourceNestedTypeVisibility = new(
        SuppressionIds.EventSourceNestedTypeVisibility,
        "CA1034",
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceNestedTypeVisibilityJustification)));

    /// <inheritdoc />
    public override void ReportSuppressions(
        SuppressionAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var compilation = context.Compilation;
        var wellKnownTypes = new WellKnownTypeSymbols(compilation);

        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            var location = diagnostic.Location;
            if (!location.IsInSource)
            {
                continue;
            }

            var tree = location.SourceTree;

            var rootNode = tree.GetRoot(cancellationToken);
            if (rootNode.FindNode(location.SourceSpan) is not ClassDeclarationSyntax nestedClassDecl)
            {
                continue;
            }

            var className = nestedClassDecl.Identifier.ValueText;
            if (className is not ("Keywords" or "Tasks" or "Opcodes"))
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(tree);
            var nestedClassSymbol = semanticModel.GetDeclaredSymbol(nestedClassDecl, cancellationToken);
            if (nestedClassSymbol?.ContainingType is not { } eventSourceType)
            {
                continue;
            }

            if (!eventSourceType.IsDerivedFrom(wellKnownTypes.EventSource))
            {
                continue;
            }

            context.ReportSuppression(Suppression.Create(EventSourceNestedTypeVisibility, diagnostic));
        }
    }

    /// <inheritdoc />
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
    [
        EventSourceNestedTypeVisibility
    ];
}
