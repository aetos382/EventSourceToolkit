using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Aetos.EventSourceToolkit.Analyzers.Properties;

namespace Aetos.EventSourceToolkit.Analyzers;

/// <summary>
/// EventSource 派生クラスは入れ子になった public な 'Keywords', 'Tasks', 'Opcodes' というクラスを要求するが
/// NetAnalyzers が CS1034 を出してうざいので、それを抑制する。
/// EventSource に関する一般的な規則なので [GeneratedEventSource] の有無は見ない。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventSourceNestedTypeVisibilitySuppressor :
    DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor EventSourceNestedTypeVisibility = new(
        SuppressionIds.EventSourceNestedTypeVisibility,
        "CA1034",
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceNestedTypeVisibilityJustification)));

    /// <inheritdoc />
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
    [
        EventSourceNestedTypeVisibility
    ];

    /// <inheritdoc />
    public override void ReportSuppressions(
        SuppressionAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var wellKnownSymbols = new WellKnownSymbols(context.Compilation);

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

            if (!eventSourceType.IsDerivedFrom(wellKnownSymbols.EventSource))
            {
                continue;
            }

            context.ReportSuppression(Suppression.Create(EventSourceNestedTypeVisibility, diagnostic));
        }
    }
}
