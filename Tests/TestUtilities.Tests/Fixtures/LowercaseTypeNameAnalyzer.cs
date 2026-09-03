using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

/// <summary>名前が小文字で始まるクラスを報告する、テスト用の Analyzer。</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class LowercaseTypeNameAnalyzer :
    DiagnosticAnalyzer
{
    public const string DiagnosticId = "TEST0001";

    private static readonly DiagnosticDescriptor Descriptor = new(
        DiagnosticId,
        "型名が小文字で始まっている",
        "型 '{0}' の名前が小文字で始まっている",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptor];

    /// <inheritdoc />
    public override void Initialize(
        AnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
    }

    private static void Analyze(
        SyntaxNodeAnalysisContext context)
    {
        var identifier = ((ClassDeclarationSyntax)context.Node).Identifier;
        var name = identifier.ValueText;

        if (name.Length == 0 || !char.IsLower(name[0]))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(Descriptor, identifier.GetLocation(), name));
    }
}
