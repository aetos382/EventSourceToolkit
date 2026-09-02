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

        var parameters = symbol.Parameters;
        foreach (var parameter in parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parameterType = parameter.Type;

            if (!EventSourceUtilities.IsSupportedParameterType(parameterType, wellKnownSymbols))
            {
                // TODO: diagnostic
                continue;
            }
        }
    }
}
