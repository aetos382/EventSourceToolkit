using System;
using System.Collections.Immutable;

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
    private static readonly DiagnosticDescriptor EventSourceClassMustBePartialClass = new(
        DiagnosticIds.EventSourceClassMustBePartialClass,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustBePartialClassTitle)),
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustBePartialClassMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustBePartialClassDescription)),
        DiagnosticHelpLinks.GetHelpLinkUri(DiagnosticIds.EventSourceClassMustBePartialClass));

    private static readonly DiagnosticDescriptor EventSourceClassMustNotBeFileLocalClass = new(
        DiagnosticIds.EventSourceClassMustNotBeFileLocalClass,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeFileLocalClassTitle)),
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeFileLocalClassMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeFileLocalClassDescription)),
        DiagnosticHelpLinks.GetHelpLinkUri(DiagnosticIds.EventSourceClassMustNotBeFileLocalClass));

    private static readonly DiagnosticDescriptor EventSourceClassMustNotBeAbstract = new(
        DiagnosticIds.EventSourceClassMustNotBeAbstract,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeAbstractTitle)),
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeAbstractMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeAbstractDescription)),
        DiagnosticHelpLinks.GetHelpLinkUri(DiagnosticIds.EventSourceClassMustNotBeAbstract));

    private static readonly DiagnosticDescriptor EventSourceClassMustDeriveFromEventSource = new(
        DiagnosticIds.EventSourceClassMustDeriveFromEventSource,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustDeriveFromEventSourceTitle)),
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustDeriveFromEventSourceMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustDeriveFromEventSourceDescription)),
        DiagnosticHelpLinks.GetHelpLinkUri(DiagnosticIds.EventSourceClassMustDeriveFromEventSource));

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
        EventSourceClassMustBePartialClass,
        EventSourceClassMustNotBeFileLocalClass,
        EventSourceClassMustNotBeAbstract,
        EventSourceClassMustDeriveFromEventSource
    ];

    private static void SyntaxNodeAction(
        SyntaxNodeAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var compilation = context.Compilation;
        var semanticModel = context.SemanticModel;
        var wellKnownTypes = new WellKnownSymbols(compilation);

        var node = (ClassDeclarationSyntax)context.Node;
        var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

        if (symbol is null)
        {
            return;
        }

        // [GeneratedEventSource] が付いていないクラスはチェック対象外。
        // いずれかの partial パーツについていれば、全てのパーツがチェック対象。
        if (!symbol.HasAttribute(wellKnownTypes.GeneratedEventSourceAttribute))
        {
            return;
        }

        // 生成したコードをパートとして追加できなければならない。
        // すなわち、すべての partial パーツは partial 修飾子を持ち、file 修飾子を持っていてはいけない。
        // 入れ子クラスの場合、含んでいるクラスもすべて見る。
        if (node.FindAugmentationBlocker() is var (blockingNode, reason))
        {
            var descriptor = reason switch
            {
                AugmentationBlockerReason.NotPartial => EventSourceClassMustBePartialClass,
                AugmentationBlockerReason.FileLocal => EventSourceClassMustNotBeFileLocalClass,
                _ => throw new InvalidOperationException()
            };

            var identifier = blockingNode.Identifier;

            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                identifier.GetLocation(),
                identifier.ValueText));

            return;
        }

        // どの partial パートも abstract 修飾子を持っていてはいけない。
        // 実際に abstract 修飾子を持っているノードのみが対象なので symbol ではなく syntax でチェックする。
        var abstractModifier = node.Modifiers
            .FirstOrNull(static x => x.IsKind(SyntaxKind.AbstractKeyword));

        if (abstractModifier is not null)
        {
            var identifier = node.Identifier;

            context.ReportDiagnostic(Diagnostic.Create(
                EventSourceClassMustNotBeAbstract,
                identifier.GetLocation(),
                identifier.ValueText));

            return;
        }

        // EventSource から派生していなければいけない。
        // いずれかの partial パーツで派生していればよいので symbol でチェックする。
        if (!symbol.IsDerivedFrom(wellKnownTypes.EventSource))
        {
            var identifier = node.Identifier;

            context.ReportDiagnostic(Diagnostic.Create(
                EventSourceClassMustDeriveFromEventSource,
                identifier.GetLocation(),
                identifier.ValueText));

            return;
        }
    }
}
