using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Testing;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

/// <summary>
/// ワークスペース上にプロジェクトを構成してコンパイルし、結果を <see cref="CSharpCompilerResult" /> として返します。
/// 既定ではジェネレーターも Analyzer も実行せず、Emit もしない。
/// 必要なものだけ <see cref="WithSourceGenerators" /> などで有効にする。
/// <see cref="RunAsync" /> は実行するだけ、<see cref="VerifyAsync" /> は期待値との比較まで行う。
/// </summary>
public sealed partial class CSharpCompilerDriver
{
    private readonly List<CSharpProjectState> _additionalProjects = [];

    private ImmutableArray<IIncrementalGenerator> _generators = [];

    private ImmutableArray<DiagnosticAnalyzer> _analyzers = [];

    private CodeFixProvider? _codeFixProvider;

    private CodeRefactoringProvider? _codeRefactoringProvider;

    private bool _emit;

    /// <summary>テスト対象のプロジェクト。ジェネレーター、Analyzer、CodeFix はこれに対して実行される。</summary>
    public CSharpProjectState PrimaryProject { get; } = new("TestProject");

    /// <summary>ジェネレーターおよび Analyzer に渡す追加ファイル。</summary>
    public IList<AdditionalText> AdditionalTexts { get; } = [];

    public GeneratorDriverOptions DriverOptions { get; set; } = new(
        trackIncrementalGeneratorSteps: true);

    public AnalyzerConfigOptionsProvider? OptionsProvider { get; set; }

    /// <summary>検証の対象に含めるコンパイラ診断の範囲。</summary>
    public CompilerDiagnostics CompilerDiagnostics { get; set; } = CompilerDiagnostics.Errors;

    /// <summary>
    /// マークアップ（<c>{|ID:...|}</c>）に加えて期待する診断。
    /// 位置を指定しない診断や、メッセージまで照合したい診断に使う。
    /// </summary>
    public IList<ExpectedDiagnostic> ExpectedDiagnostics { get; } = [];

    /// <summary>CodeFix / CodeRefactoring の適用後に期待するソース。キーはファイル名。</summary>
    public IDictionary<string, string> ExpectedFixedSources { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>適用する <see cref="CodeAction" /> の <see cref="CodeAction.EquivalenceKey" />。</summary>
    public string? CodeActionEquivalenceKey { get; set; }

    /// <summary>適用する <see cref="CodeAction" /> の位置。<see cref="CodeActionEquivalenceKey" /> での絞り込みの後に適用される。</summary>
    public int CodeActionIndex { get; set; }

    /// <summary>CodeFix の適用を繰り返す上限。これを超えても収束しない場合は失敗とする。</summary>
    public int MaxCodeFixIterations { get; set; } = 10;

    /// <inheritdoc cref="CSharpProjectState.Sources" />
    public IList<TestSource> Sources => this.PrimaryProject.Sources;

    /// <inheritdoc cref="CSharpProjectState.AdditionalReferences" />
    public IList<MetadataReference> AdditionalReferences => this.PrimaryProject.AdditionalReferences;

    /// <inheritdoc cref="CSharpProjectState.AssemblyName" />
    public string AssemblyName
    {
        get => this.PrimaryProject.AssemblyName;
        set => this.PrimaryProject.AssemblyName = value;
    }

    /// <inheritdoc cref="CSharpProjectState.ReferenceAssemblies" />
    public ReferenceAssemblies ReferenceAssemblies
    {
        get => this.PrimaryProject.ReferenceAssemblies;
        set => this.PrimaryProject.ReferenceAssemblies = value;
    }

    /// <inheritdoc cref="CSharpProjectState.ParseOptions" />
    public CSharpParseOptions ParseOptions
    {
        get => this.PrimaryProject.ParseOptions;
        set => this.PrimaryProject.ParseOptions = value;
    }

    /// <inheritdoc cref="CSharpProjectState.CompilationOptions" />
    public CSharpCompilationOptions CompilationOptions
    {
        get => this.PrimaryProject.CompilationOptions;
        set => this.PrimaryProject.CompilationOptions = value;
    }

    /// <inheritdoc cref="CSharpProjectState.AddSource(string)" />
    public CSharpCompilerDriver AddSource(
        string code)
    {
        this.PrimaryProject.AddSource(code);

        return this;
    }

    /// <inheritdoc cref="CSharpProjectState.AddSource(string, string)" />
    public CSharpCompilerDriver AddSource(
        string fileName,
        string code)
    {
        this.PrimaryProject.AddSource(fileName, code);

        return this;
    }

    /// <inheritdoc cref="CSharpProjectState.AddMarkupSource(string)" />
    public CSharpCompilerDriver AddMarkupSource(
        string markup)
    {
        this.PrimaryProject.AddMarkupSource(markup);

        return this;
    }

    /// <inheritdoc cref="CSharpProjectState.AddMarkupSource(string, string)" />
    public CSharpCompilerDriver AddMarkupSource(
        string fileName,
        string markup)
    {
        this.PrimaryProject.AddMarkupSource(fileName, markup);

        return this;
    }

    /// <inheritdoc cref="CSharpProjectState.AddReference(MetadataReference)" />
    public CSharpCompilerDriver AddReference(
        MetadataReference reference)
    {
        this.PrimaryProject.AddReference(reference);

        return this;
    }

    /// <summary>
    /// テスト対象のプロジェクトから参照される別のプロジェクトを追加します。
    /// 別アセンブリにある型をジェネレーターに解決させたい場合に使う。
    /// </summary>
    public CSharpCompilerDriver AddProject(
        string name,
        Action<CSharpProjectState> configure)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(configure);

        var project = new CSharpProjectState(name);

        configure(project);

        this._additionalProjects.Add(project);

        return this;
    }

    /// <summary>インクリメンタル ジェネレーターを実行します。</summary>
    public CSharpCompilerDriver WithSourceGenerators(
        params IIncrementalGenerator[] generators)
    {
        ArgumentNullException.ThrowIfNull(generators);

        this._generators = [.. generators];

        return this;
    }

    /// <summary>ジェネレーターの実行後の <see cref="Compilation" /> に対して Analyzer を実行します。</summary>
    public CSharpCompilerDriver WithAnalyzers(
        params DiagnosticAnalyzer[] analyzers)
    {
        ArgumentNullException.ThrowIfNull(analyzers);

        this._analyzers = [.. analyzers];

        return this;
    }

    /// <summary>報告された診断に対して CodeFix を、変化しなくなるまで繰り返し適用します。</summary>
    public CSharpCompilerDriver WithCodeFix(
        CodeFixProvider codeFixProvider)
    {
        ArgumentNullException.ThrowIfNull(codeFixProvider);

        this._codeFixProvider = codeFixProvider;

        return this;
    }

    /// <summary>
    /// マークアップで示した位置（<c>[|...|]</c> または <c>$$</c>）に CodeRefactoring を 1 回適用します。
    /// 適用は解析より前に行われる。
    /// </summary>
    public CSharpCompilerDriver WithCodeRefactoring(
        CodeRefactoringProvider codeRefactoringProvider)
    {
        ArgumentNullException.ThrowIfNull(codeRefactoringProvider);

        this._codeRefactoringProvider = codeRefactoringProvider;

        return this;
    }

    /// <summary>アセンブリを Emit します。</summary>
    public CSharpCompilerDriver WithEmit(
        bool emit = true)
    {
        this._emit = emit;

        return this;
    }

    /// <summary>期待する診断を追加します。</summary>
    public CSharpCompilerDriver ExpectDiagnostic(
        ExpectedDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        this.ExpectedDiagnostics.Add(diagnostic);

        return this;
    }

    /// <summary>CodeFix / CodeRefactoring の適用後に期待するソースを指定します。</summary>
    public CSharpCompilerDriver ExpectFixedSource(
        string fileName,
        string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentNullException.ThrowIfNull(code);

        this.ExpectedFixedSources[fileName] = code;

        return this;
    }

    /// <summary>構成した内容を実行します。期待値との比較は行わない。</summary>
    public async Task<CSharpCompilerResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        if (this.PrimaryProject.Sources.Count == 0)
        {
            throw new InvalidOperationException("ソースが 1 つも追加されていません。");
        }

        if (this._codeFixProvider is not null && this._codeRefactoringProvider is not null)
        {
            throw new InvalidOperationException("CodeFix と CodeRefactoring を同時に適用することはできません。");
        }

        var workspace = new AdhocWorkspace();

        try
        {
            var project = await this.CreateSolutionAsync(workspace, cancellationToken).ConfigureAwait(false);

            // 期待する診断はマークアップ、つまり修正前のソースに書かれているため、
            // 解析は CodeFix / CodeRefactoring を適用する前に行う。
            var analysis = await this.AnalyzeAsync(project, cancellationToken).ConfigureAwait(false);

            var finalAnalysis = analysis;

            if (this._codeRefactoringProvider is not null)
            {
                project = await this.ApplyCodeRefactoringAsync(project, cancellationToken).ConfigureAwait(false);

                finalAnalysis = await this.AnalyzeAsync(project, cancellationToken).ConfigureAwait(false);
            }

            if (this._codeFixProvider is not null)
            {
                (project, finalAnalysis) = await this
                    .ApplyCodeFixesAsync(project, analysis, cancellationToken)
                    .ConfigureAwait(false);
            }

            var finalSources = await GetSourcesAsync(project, cancellationToken).ConfigureAwait(false);

            var (emitResult, assemblyImage) = this.Emit(
                finalAnalysis.OutputCompilation, cancellationToken);

            return new(
                workspace, project, analysis, finalAnalysis, finalSources, emitResult, assemblyImage);
        }
        catch
        {
            workspace.Dispose();

            throw;
        }
    }

    /// <summary>構成した内容を実行し、期待する診断および修正後のソースと比較します。</summary>
    /// <exception cref="TestVerificationException">期待値と一致しない場合。</exception>
    public async Task<CSharpCompilerResult> VerifyAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await this.RunAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            this.VerifyDiagnostics(result);
            this.VerifyFixedSources(result);
        }
        catch
        {
            result.Dispose();

            throw;
        }

        return result;
    }

    private (EmitResult? Result, ImmutableArray<byte> Image) Emit(
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        if (!this._emit)
        {
            return (null, []);
        }

        using var stream = new MemoryStream();

        var emitResult = compilation.Emit(stream, cancellationToken: cancellationToken);

        return (emitResult, emitResult.Success ? [.. stream.ToArray()] : []);
    }
}
