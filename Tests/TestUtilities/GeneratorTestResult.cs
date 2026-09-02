using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

/// <summary><see cref="CSharpGeneratorRunner" /> の実行結果を表します。</summary>
public sealed class GeneratorTestResult
{
    internal GeneratorTestResult(
        GeneratorDriverRunResult runResult,
        Compilation inputCompilation,
        Compilation outputCompilation,
        ImmutableArray<GeneratedSourceFile> generatedSources)
    {
        this.RunResult = runResult;
        this.InputCompilation = inputCompilation;
        this.OutputCompilation = outputCompilation;
        this.GeneratedSources = generatedSources;
    }

    /// <summary>ドライバーの生の実行結果。インクリメンタル ステップの検証などに使う。</summary>
    public GeneratorDriverRunResult RunResult { get; }

    /// <summary>ジェネレーターを実行する前のコンパイル。</summary>
    public Compilation InputCompilation { get; }

    /// <summary>生成されたソースを追加した後のコンパイル。</summary>
    public Compilation OutputCompilation { get; }

    /// <summary>生成されたすべてのソース。</summary>
    public ImmutableArray<GeneratedSourceFile> GeneratedSources { get; }

    /// <summary>生成されたすべてのソースのファイル名。</summary>
    public ImmutableArray<string> GeneratedFileNames =>
        [.. this.GeneratedSources.Select(static x => x.FileName)];

    /// <summary>ジェネレーター自身が報告した診断。</summary>
    public ImmutableArray<Diagnostic> GeneratorDiagnostics => this.RunResult.Diagnostics;

    /// <summary>生成後のコンパイルの診断。コンパイル エラーの検証に使う。</summary>
    public ImmutableArray<Diagnostic> GetCompilationDiagnostics(
        CancellationToken cancellationToken = default)
    {
        return this.OutputCompilation.GetDiagnostics(cancellationToken);
    }

    /// <summary>指定されたファイル名で生成されたソースを返します。生成されていない場合は <see langword="null" />。</summary>
    public GeneratedSourceFile? FindGeneratedSource(
        string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        var matches = this.GeneratedSources
            .Where(x => string.Equals(x.FileName, fileName, StringComparison.Ordinal))
            .ToArray();

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"'{fileName}' が複数のジェネレーターから生成されています。{nameof(this.GeneratedSources)} を {nameof(GeneratedSourceFile.FilePath)} で絞り込んでください。");
        }

        return matches.Length == 0 ? null : matches[0];
    }

    /// <summary>指定されたファイル名で生成されたソースのテキストを返します。生成されていない場合は例外。</summary>
    public string GetGeneratedText(
        string fileName)
    {
        var source = this.FindGeneratedSource(fileName);

        if (source is null)
        {
            var generatedFileNames = string.Join(", ", this.GeneratedFileNames);

            throw new InvalidOperationException(
                $"'{fileName}' は生成されていません。生成されたのは [{generatedFileNames}] です。");
        }

        return source.Text;
    }
}
