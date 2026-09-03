using System;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using Shouldly;

using Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests;

[TestClass]
public sealed class CSharpCompilerDriverTest
{
    /// <summary>
    /// 既定の構成でソースがコンパイルでき、参照アセンブリが解決されていることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ソースをコンパイルできる()
    {
        var driver = new CSharpCompilerDriver()
            .AddSource("Test0.cs", "public class C { public string M() => string.Empty; }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.CompilerDiagnostics.ShouldBeEmpty();
        result.OutputCompilation.GetTypeByMetadataName("C").ShouldNotBeNull();
        result.GeneratorRunResult.ShouldBeNull();
        result.EmitResult.ShouldBeNull();
    }

    /// <summary>
    /// ジェネレーターを実行しない場合、生成前後のコンパイルが同一であることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ジェネレーターを実行しなければ生成前後のコンパイルは同一になる()
    {
        var driver = new CSharpCompilerDriver()
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.OutputCompilation.ShouldBeSameAs(result.InputCompilation);
        result.GeneratedSources.ShouldBeEmpty();
    }

    /// <summary>
    /// 既定ではコンパイル エラーが検証対象に含まれ、
    /// <see cref="CSharpCompilerDriver.CompilerDiagnostics" /> を
    /// <see cref="CompilerDiagnostics.None" /> にすると除外されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task コンパイルエラーは既定で検証対象に含まれる()
    {
        const string Code = "public class C { public void M() { return 1; } }";

        var driver = new CSharpCompilerDriver().AddSource("Test0.cs", Code);

        using var withErrors = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        withErrors.CompilerDiagnostics.ShouldNotBeEmpty();

        driver.CompilerDiagnostics = CompilerDiagnostics.None;

        using var withoutErrors = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        withoutErrors.CompilerDiagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// <see cref="CompilerDiagnostics.Warnings" /> を指定すると警告まで含まれ、
    /// 既定の <see cref="CompilerDiagnostics.Errors" /> では含まれないことを確認する。
    /// </summary>
    [TestMethod]
    public async Task 警告を含めるかどうかを切り替えられる()
    {
        // CS0169: フィールドが使用されていない。
        const string Code = "public class C { private int field; }";

        var driver = new CSharpCompilerDriver().AddSource("Test0.cs", Code);

        using var errorsOnly = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        errorsOnly.CompilerDiagnostics.ShouldBeEmpty();

        driver.CompilerDiagnostics = CompilerDiagnostics.Warnings;

        using var withWarnings = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        withWarnings.CompilerDiagnostics.ShouldContain(static x => x.Id == "CS0169");
    }

    /// <summary>
    /// <see cref="CSharpCompilerDriver.AddProject" /> で追加したプロジェクトの型が、
    /// テスト対象のプロジェクトから参照できることを確認する。
    /// </summary>
    [TestMethod]
    public async Task 追加したプロジェクトの型を参照できる()
    {
        var driver = new CSharpCompilerDriver()
            .AddProject(
                "Library",
                static x => x.AddSource("Library.cs", "namespace Lib; public class Base { }"))
            .AddSource("Test0.cs", "public class C : Lib.Base { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.CompilerDiagnostics.ShouldBeEmpty();

        result.OutputCompilation
            .GetTypeByMetadataName("C")
            .ShouldNotBeNull()
            .BaseType.ShouldNotBeNull()
            .ContainingAssembly.Name.ShouldBe("Library");
    }

    /// <summary>
    /// <see cref="CSharpCompilerDriver.WithEmit" /> した結果の
    /// <see cref="CSharpCompilerResult.GetMetadataReference" /> を、
    /// 別のドライバーの参照として使えることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Emitした結果を別のドライバーから参照できる()
    {
        var libraryDriver = new CSharpCompilerDriver
        {
            AssemblyName = "Library"
        };

        libraryDriver
            .WithEmit()
            .AddSource("Library.cs", "namespace Lib; public class Base { }");

        using var libraryResult = await libraryDriver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        libraryResult.EmitResult.ShouldNotBeNull().Success.ShouldBeTrue();
        libraryResult.AssemblyImage.ShouldNotBeEmpty();

        var driver = new CSharpCompilerDriver()
            .AddReference(libraryResult.GetMetadataReference())
            .AddSource("Test0.cs", "public class C : Lib.Base { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.CompilerDiagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// Emit していない結果から <see cref="CSharpCompilerResult.GetMetadataReference" /> を
    /// 呼び出すと、セットアップの誤りとして例外になることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Emitしていなければメタデータ参照を取得できない()
    {
        var driver = new CSharpCompilerDriver()
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        Should.Throw<InvalidOperationException>(() => result.GetMetadataReference());
    }

    /// <summary>ソースを 1 つも追加していない場合、セットアップの誤りとして例外になることを確認する。</summary>
    [TestMethod]
    public async Task ソースがなければ例外になる()
    {
        var driver = new CSharpCompilerDriver();

        await Should
            .ThrowAsync<InvalidOperationException>(
                () => driver.RunAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// マークアップを <see cref="CSharpCompilerDriver.AddSource(string, string)" /> に
    /// 渡してしまった場合、構文エラーとして実行前に落ちることを確認する。
    /// </summary>
    [TestMethod]
    public async Task マークアップをそのまま渡すと構文エラーになる()
    {
        var driver = new CSharpCompilerDriver()
            .AddSource("Test0.cs", "public class {|TEST0001:c|} { }");

        var exception = await Should
            .ThrowAsync<InvalidOperationException>(
                () => driver.RunAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);

        exception.Message.ShouldContain("Test0.cs");
    }

    /// <summary>同じファイル名のソースを追加した場合、セットアップの誤りとして例外になることを確認する。</summary>
    [TestMethod]
    public async Task ファイル名が重複していれば例外になる()
    {
        var driver = new CSharpCompilerDriver()
            .AddSource("Test0.cs", "public class C { }")
            .AddSource("Test0.cs", "public class D { }");

        var exception = await Should
            .ThrowAsync<InvalidOperationException>(
                () => driver.RunAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);

        exception.Message.ShouldContain("Test0.cs");
    }

    /// <summary>
    /// CodeFix と CodeRefactoring を同時に構成した場合、セットアップの誤りとして例外になることを確認する。
    /// </summary>
    [TestMethod]
    public async Task CodeFixとCodeRefactoringは同時に構成できない()
    {
        var driver = new CSharpCompilerDriver()
            .WithCodeFix(new CapitalizeTypeNameCodeFix())
            .WithCodeRefactoring(new RenameTypeCodeRefactoring())
            .AddSource("Test0.cs", "public class C { }");

        await Should
            .ThrowAsync<InvalidOperationException>(
                () => driver.RunAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);
    }

    public CSharpCompilerDriverTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
