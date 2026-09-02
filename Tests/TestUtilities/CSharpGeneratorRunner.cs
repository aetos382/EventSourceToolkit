using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

/// <summary>
/// インクリメンタル ジェネレーターを実行し、結果を <see cref="GeneratorTestResult" /> として返します。
/// 期待値との比較は呼び出し側で行う。
/// </summary>
public sealed class CSharpGeneratorRunner
{
    private readonly ImmutableArray<IIncrementalGenerator> _generators;

    public CSharpGeneratorRunner(
        IIncrementalGenerator primaryGenerator,
        params IIncrementalGenerator[] restGenerators)
    {
        ArgumentNullException.ThrowIfNull(primaryGenerator);

        this._generators = [primaryGenerator, .. restGenerators];
    }

    /// <summary>コンパイル対象のソース。</summary>
    public IList<(string FileName, string Code)> Sources { get; } = [];

    /// <summary>ジェネレーターに渡す追加ファイル。</summary>
    public IList<AdditionalText> AdditionalTexts { get; } = [];

    public string AssemblyName { get; set; } = "test";

    public ReferenceAssemblies ReferenceAssemblies { get; set; } = ReferenceAssemblies.Net.Net100;

    public CSharpParseOptions ParseOptions { get; set; } = CSharpParseOptions.Default;

    public CSharpCompilationOptions CompilationOptions { get; set; } = new(
        OutputKind.DynamicallyLinkedLibrary,
        allowUnsafe: true,
        nullableContextOptions: NullableContextOptions.Enable);

    public GeneratorDriverOptions DriverOptions { get; set; } = new(
        trackIncrementalGeneratorSteps: true);

    public AnalyzerConfigOptionsProvider? OptionsProvider { get; set; }

    /// <summary>自動採番したファイル名でソースを追加します。</summary>
    public CSharpGeneratorRunner AddSource(
        string code)
    {
        return this.AddSource($"Test{this.Sources.Count}.cs", code);
    }

    public CSharpGeneratorRunner AddSource(
        string fileName,
        string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentNullException.ThrowIfNull(code);

        this.Sources.Add((fileName, code));

        return this;
    }

    public async Task<GeneratorTestResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        if (this.Sources.Count == 0)
        {
            throw new InvalidOperationException("ソースが 1 つも追加されていません。");
        }

        var parseOptions = this.ParseOptions;

        var references = await this.ReferenceAssemblies
            .ResolveAsync(LanguageNames.CSharp, cancellationToken)
            .ConfigureAwait(false);

        var syntaxTrees = this.Sources
            .Select(x => CSharpSyntaxTree.ParseText(
                x.Code, parseOptions, x.FileName, Encoding.UTF8, cancellationToken))
            .ToArray();

        var inputCompilation = CSharpCompilation.Create(
            this.AssemblyName,
            syntaxTrees,
            references,
            this.CompilationOptions);

        var driver = (GeneratorDriver)CSharpGeneratorDriver.Create(
            this._generators.Select(static x => x.AsSourceGenerator()),
            additionalTexts: this.AdditionalTexts,
            parseOptions: parseOptions,
            optionsProvider: this.OptionsProvider,
            driverOptions: this.DriverOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            inputCompilation,
            out var outputCompilation,
            out _,
            cancellationToken);

        var runResult = driver.GetRunResult();

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

        return new(
            runResult,
            inputCompilation,
            outputCompilation,
            generatedSources.ToImmutable());
    }
}
