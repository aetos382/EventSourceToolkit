using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

public sealed partial class CSharpCompilerDriver
{
    /// <summary>
    /// プロジェクトをコンパイルし、ジェネレーターと Analyzer を実行して診断を集めます。
    /// CodeFix の反復のたびに呼ばれる。
    /// </summary>
    private async Task<CSharpAnalysisResult> AnalyzeAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        var inputCompilation = await project
            .GetCompilationAsync(cancellationToken)
            .ConfigureAwait(false);

        if (inputCompilation is null)
        {
            throw new InvalidOperationException(
                $"プロジェクト '{project.Name}' の {nameof(Compilation)} を取得できませんでした。");
        }

        var outputCompilation = inputCompilation;

        GeneratorDriverRunResult? generatorRunResult = null;
        var generatedSources = ImmutableArray<GeneratedSourceFile>.Empty;

        if (!this._generators.IsEmpty)
        {
            var driver = (GeneratorDriver)CSharpGeneratorDriver.Create(
                this._generators.Select(static x => x.AsSourceGenerator()),
                additionalTexts: this.AdditionalTexts,
                parseOptions: this.ParseOptions,
                optionsProvider: this.OptionsProvider,
                driverOptions: this.DriverOptions);

            driver = driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out outputCompilation,
                out _,
                cancellationToken);

            generatorRunResult = driver.GetRunResult();

            generatedSources = await CreateGeneratedSourcesAsync(
                generatorRunResult, cancellationToken).ConfigureAwait(false);
        }

        var analyzerDiagnostics = ImmutableArray<Diagnostic>.Empty;

        if (!this._analyzers.IsEmpty)
        {
            analyzerDiagnostics = await outputCompilation
                .WithAnalyzers(this._analyzers, this.CreateAnalyzerOptions())
                .GetAnalyzerDiagnosticsAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var compilerDiagnostics = outputCompilation
            .GetDiagnostics(cancellationToken)
            .Where(x => IsIncluded(x, this.CompilerDiagnostics))
            .ToImmutableArray();

        return new(
            inputCompilation,
            outputCompilation,
            generatedSources,
            generatorRunResult,
            analyzerDiagnostics,
            compilerDiagnostics);
    }

    private AnalyzerOptions CreateAnalyzerOptions()
    {
        return new(
            [.. this.AdditionalTexts],
            this.OptionsProvider ?? new TestAnalyzerConfigOptionsProvider());
    }

    private static async Task<ImmutableArray<GeneratedSourceFile>> CreateGeneratedSourcesAsync(
        GeneratorDriverRunResult runResult,
        CancellationToken cancellationToken)
    {
        var generatedSources = ImmutableArray.CreateBuilder<GeneratedSourceFile>(
            runResult.GeneratedTrees.Length);

        foreach (var tree in runResult.GeneratedTrees)
        {
            var text = await tree.GetTextAsync(cancellationToken).ConfigureAwait(false);

            generatedSources.Add(new(
                Path.GetFileName(tree.FilePath),
                tree.FilePath,
                text.ToString()));
        }

        return generatedSources.MoveToImmutable();
    }

    private static bool IsIncluded(
        Diagnostic diagnostic,
        CompilerDiagnostics compilerDiagnostics)
    {
        return compilerDiagnostics switch
        {
            CompilerDiagnostics.None => false,
            CompilerDiagnostics.Errors => diagnostic.Severity >= DiagnosticSeverity.Error,
            CompilerDiagnostics.Warnings => diagnostic.Severity >= DiagnosticSeverity.Warning,
            CompilerDiagnostics.Suggestions => diagnostic.Severity >= DiagnosticSeverity.Info,
            CompilerDiagnostics.All => true,
            _ => throw new ArgumentOutOfRangeException(nameof(compilerDiagnostics))
        };
    }
}
