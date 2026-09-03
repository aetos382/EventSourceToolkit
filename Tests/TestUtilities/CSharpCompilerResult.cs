using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

/// <summary>
/// <see cref="CSharpCompilerDriver" /> の実行結果を表します。
/// ワークスペースを保持するため、使い終わったら破棄する。
/// </summary>
public sealed class CSharpCompilerResult :
    IDisposable
{
    private readonly AdhocWorkspace _workspace;

    private readonly CSharpAnalysisResult _analysis;

    private readonly CSharpAnalysisResult _finalAnalysis;

    private MetadataReference? _metadataReference;

    internal CSharpCompilerResult(
        AdhocWorkspace workspace,
        Project project,
        CSharpAnalysisResult analysis,
        CSharpAnalysisResult finalAnalysis,
        ImmutableArray<(string FileName, string Text)> finalSources,
        EmitResult? emitResult,
        ImmutableArray<byte> assemblyImage)
    {
        this._workspace = workspace;
        this._analysis = analysis;
        this._finalAnalysis = finalAnalysis;

        this.Project = project;
        this.FinalSources = finalSources;
        this.EmitResult = emitResult;
        this.AssemblyImage = assemblyImage;
    }

    /// <summary>
    /// テスト対象のプロジェクト。CodeFix / CodeRefactoring を適用した場合は適用後のもの。
    /// </summary>
    public Project Project { get; }

    /// <summary>テスト対象のプロジェクトを含むソリューション。</summary>
    public Solution Solution => this.Project.Solution;

    /// <summary>ジェネレーターを実行する前のコンパイル。</summary>
    public Compilation InputCompilation => this._analysis.InputCompilation;

    /// <summary>
    /// 生成されたソースを追加した後のコンパイル。
    /// ジェネレーターを実行していない場合は <see cref="InputCompilation" /> と同一。
    /// </summary>
    public Compilation OutputCompilation => this._analysis.OutputCompilation;

    /// <summary>生成されたすべてのソース。</summary>
    public ImmutableArray<GeneratedSourceFile> GeneratedSources => this._analysis.GeneratedSources;

    /// <summary>
    /// ドライバーの生の実行結果。インクリメンタル ステップの検証などに使う。
    /// ジェネレーターを実行していない場合は <see langword="null" />。
    /// </summary>
    public GeneratorDriverRunResult? GeneratorRunResult => this._analysis.GeneratorRunResult;

    /// <summary>ジェネレーター自身が報告した診断。</summary>
    public ImmutableArray<Diagnostic> GeneratorDiagnostics =>
        this._analysis.GeneratorRunResult?.Diagnostics ?? [];

    /// <summary>Analyzer が報告した診断。Analyzer を実行していない場合は空。</summary>
    public ImmutableArray<Diagnostic> AnalyzerDiagnostics => this._analysis.AnalyzerDiagnostics;

    /// <summary>
    /// コンパイラが報告した診断。
    /// <see cref="CSharpCompilerDriver.CompilerDiagnostics" /> で絞り込まれている。
    /// </summary>
    public ImmutableArray<Diagnostic> CompilerDiagnostics => this._analysis.CompilerDiagnostics;

    /// <summary>
    /// 検証の対象となるすべての診断。
    /// CodeFix / CodeRefactoring を適用した場合も、適用前のソースに対するもの。
    /// 期待する診断はマークアップ、つまり適用前のソースに書かれているため。
    /// </summary>
    public ImmutableArray<Diagnostic> AllDiagnostics => this._analysis.AllDiagnostics;

    /// <summary>
    /// CodeFix / CodeRefactoring を適用した後に残った診断。
    /// 適用していない場合は <see cref="AllDiagnostics" /> と同じ。
    /// </summary>
    public ImmutableArray<Diagnostic> RemainingDiagnostics => this._finalAnalysis.AllDiagnostics;

    /// <summary>
    /// CodeFix / CodeRefactoring を適用した後のコンパイル。Emit の対象でもある。
    /// 適用していない場合は <see cref="OutputCompilation" /> と同一。
    /// </summary>
    public Compilation FinalCompilation => this._finalAnalysis.OutputCompilation;

    /// <summary>
    /// プロジェクトにある手書きのソース。
    /// CodeFix / CodeRefactoring を適用した場合は適用後のもの。生成されたソースは含まない。
    /// </summary>
    public ImmutableArray<(string FileName, string Text)> FinalSources { get; }

    /// <summary>Emit の結果。Emit していない場合は <see langword="null" />。</summary>
    public EmitResult? EmitResult { get; }

    /// <summary>Emit されたアセンブリのイメージ。Emit していない、または失敗した場合は空。</summary>
    public ImmutableArray<byte> AssemblyImage { get; }

    /// <summary>生成されたすべてのソースのファイル名。</summary>
    public ImmutableArray<string> GeneratedFileNames =>
        [.. this.GeneratedSources.Select(static x => x.FileName)];

    /// <summary>生成後のコンパイルのすべての診断。絞り込みは行わない。</summary>
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

    /// <summary>
    /// Emit されたアセンブリを参照する <see cref="MetadataReference" /> を返します。
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Emit していない場合、または Emit に失敗した場合。
    /// </exception>
    public MetadataReference GetMetadataReference()
    {
        if (this._metadataReference is not null)
        {
            return this._metadataReference;
        }

        var emitResult = this.EmitResult;

        if (emitResult is null)
        {
            throw new InvalidOperationException(
                $"Emit していません。{nameof(CSharpCompilerDriver)}.{nameof(CSharpCompilerDriver.WithEmit)} を呼び出してください。");
        }

        if (!emitResult.Success)
        {
            var messages = string.Join(
                Environment.NewLine,
                emitResult.Diagnostics
                    .Where(static x => x.Severity == DiagnosticSeverity.Error)
                    .Select(static x => x.ToString()));

            throw new InvalidOperationException(
                $"アセンブリ '{this.OutputCompilation.AssemblyName}' のコンパイルに失敗しました。{Environment.NewLine}{messages}");
        }

        this._metadataReference = MetadataReference.CreateFromImage(
            this.AssemblyImage,
            filePath: $"{this.OutputCompilation.AssemblyName}.dll");

        return this._metadataReference;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this._workspace.Dispose();
    }
}
