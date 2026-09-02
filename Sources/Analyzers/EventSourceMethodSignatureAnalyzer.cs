using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Aetos.EventSourceToolkit.Analyzers.Properties;

namespace Aetos.EventSourceToolkit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventSourceMethodSignatureAnalyzer :
    DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor ParameterTypeNotSupported = new(
        DiagnosticIds.ParameterTypeNotSupported,
        Resources.GetLocalizableResourceString(nameof(Resources.ParameterTypeNotSupportedTitle)),
        Resources.GetLocalizableResourceString(nameof(Resources.ParameterTypeNotSupportedMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        Resources.GetLocalizableResourceString(nameof(Resources.ParameterTypeNotSupportedDescription)),
        DiagnosticHelpLinks.GetHelpLinkUri(DiagnosticIds.ParameterTypeNotSupported));

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.MethodDeclaration);
    }

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        ParameterTypeNotSupported
    ];

    private static void SyntaxNodeAction(
        SyntaxNodeAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var compilation = context.Compilation;
        var semanticModel = context.SemanticModel;
        var wellKnownSymbols = new WellKnownSymbols(compilation);

        var node = (MethodDeclarationSyntax)context.Node;
        var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

        if (symbol is null)
        {
            return;
        }

        // partial メソッドでない、または既に実装がある場合は検査対象外
        if (!node.HasPartialModifier ||
            !symbol.IsPartialDefinition ||
            symbol.PartialImplementationPart is not null)
        {
            return;
        }

        // 包含クラスが拡張可能でなければ検査対象外
        if (node.Parent is not ClassDeclarationSyntax { CanBeAugmented: true })
        {
            return;
        }

        var containingType = symbol.ContainingType!;

        // 型に [GeneratedEventSource] がついていなければ検査対象外
        if (!containingType.HasAttribute(wellKnownSymbols.GeneratedEventSourceAttribute))
        {
            return;
        }

        // 型が EventSource から派生していなければ検査対象外
        if (!containingType.IsDerivedFrom(wellKnownSymbols.EventSource))
        {
            return;
        }

        // [NonEvent] がついているメソッドは検査対象外
        if (symbol.HasAttribute(wellKnownSymbols.NonEventAttribute))
        {
            return;
        }

        // [Event] がついていないメソッドは付けるといいよ
        var eventAttribute = symbol.GetAttribute(wellKnownSymbols.EventAttribute);
        if (eventAttribute is null)
        {
            // TODO: Diagnostics
        }
        else
        {
            if (symbol.IsStatic)
            {
                // [Event] がついているなら static メソッドはダメよ
                // TODO: diagnostic
            }
        }

        var parameters = node.ParameterList.Parameters;
        foreach (var parameter in parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parameterTypeNode = parameter.Type!;
            var parameterSymbolInfo = semanticModel.GetSymbolInfo(parameterTypeNode, cancellationToken);
            var parameterTypeSymbol = (ITypeSymbol)parameterSymbolInfo.Symbol!;

            if (!EventSourceUtilities.IsSupportedParameterType(parameterTypeSymbol, wellKnownSymbols))
            {
                var parameterTypeName = parameterTypeNode.SyntaxTree.GetText(cancellationToken).GetSubText(parameterTypeNode.Span);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ParameterTypeNotSupported,
                        parameter.GetLocation(),
                        parameter.Identifier.ValueText,
                        parameterTypeName));

                continue;
            }
        }
    }
}
