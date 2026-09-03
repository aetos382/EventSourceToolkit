using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

/// <summary>ワークスペースに作成するプロジェクト 1 つ分の構成。</summary>
public sealed class CSharpProjectState
{
    public CSharpProjectState(
        string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        this.Name = name;
        this.AssemblyName = name;
    }

    public string Name { get; }

    public string AssemblyName { get; set; }

    /// <summary>コンパイル対象のソース。</summary>
    public IList<TestSource> Sources { get; } = [];

    /// <summary><see cref="ReferenceAssemblies" /> に加えて参照するアセンブリ。</summary>
    public IList<MetadataReference> AdditionalReferences { get; } = [];

    public ReferenceAssemblies ReferenceAssemblies { get; set; } = ReferenceAssemblies.Net.Net100;

    public CSharpParseOptions ParseOptions { get; set; } = CSharpParseOptions.Default;

    public CSharpCompilationOptions CompilationOptions { get; set; } = new(
        OutputKind.DynamicallyLinkedLibrary,
        allowUnsafe: true,
        nullableContextOptions: NullableContextOptions.Enable);

    /// <summary>自動採番したファイル名で、マークアップを解析せずにソースを追加します。</summary>
    public CSharpProjectState AddSource(
        string code)
    {
        return this.AddSource($"Test{this.Sources.Count}.cs", code);
    }

    /// <summary>マークアップを解析せずにソースを追加します。</summary>
    public CSharpProjectState AddSource(
        string fileName,
        string code)
    {
        this.Sources.Add(TestSource.FromCode(fileName, code));

        return this;
    }

    /// <summary>自動採番したファイル名で、テスト マークアップを含むソースを追加します。</summary>
    public CSharpProjectState AddMarkupSource(
        string markup)
    {
        return this.AddMarkupSource($"Test{this.Sources.Count}.cs", markup);
    }

    /// <summary>テスト マークアップを含むソースを追加します。</summary>
    public CSharpProjectState AddMarkupSource(
        string fileName,
        string markup)
    {
        this.Sources.Add(TestSource.FromMarkup(fileName, markup));

        return this;
    }

    public CSharpProjectState AddReference(
        MetadataReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        this.AdditionalReferences.Add(reference);

        return this;
    }
}
