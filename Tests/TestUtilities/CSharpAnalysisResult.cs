using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

/// <summary>プロジェクト 1 回分の解析結果。CodeFix の反復ごとに作り直される。</summary>
internal sealed record CSharpAnalysisResult(
    Compilation InputCompilation,
    Compilation OutputCompilation,
    ImmutableArray<GeneratedSourceFile> GeneratedSources,
    GeneratorDriverRunResult? GeneratorRunResult,
    ImmutableArray<Diagnostic> AnalyzerDiagnostics,
    ImmutableArray<Diagnostic> CompilerDiagnostics)
{
    /// <summary>ジェネレーター、Analyzer、コンパイラのすべての診断。</summary>
    public ImmutableArray<Diagnostic> AllDiagnostics =>
    [
        .. this.GeneratorRunResult?.Diagnostics ?? [],
        .. this.AnalyzerDiagnostics,
        .. this.CompilerDiagnostics
    ];
}
