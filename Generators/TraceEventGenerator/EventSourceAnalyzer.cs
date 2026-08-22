using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aetos.Tracing;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventSourceAnalyzer :
    DiagnosticAnalyzer
{
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
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [];

    private static void SyntaxNodeAction(
        SyntaxNodeAnalysisContext context)
    {
    }
}
