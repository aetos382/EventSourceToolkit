using System;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using Shouldly;

using Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests;

[TestClass]
public sealed class CSharpCompilerDriverDiagnosticTest
{
    /// <summary>Analyzer が報告した診断が取得できることを確認する。</summary>
    [TestMethod]
    public async Task Analyzerの診断を取得できる()
    {
        var driver = new CSharpCompilerDriver()
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .AddSource("Test0.cs", "public class c { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.AnalyzerDiagnostics.ShouldHaveSingleItem()
            .Id.ShouldBe(LowercaseTypeNameAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Analyzer は生成後のコンパイルに対して実行されるため、
    /// 生成されたソースも解析の対象になりうることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Analyzerは生成後のコンパイルに対して実行される()
    {
        var driver = new CSharpCompilerDriver()
            .WithSourceGenerators(new LowercaseClassGenerator())
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.AnalyzerDiagnostics.ShouldHaveSingleItem()
            .Location.SourceTree.ShouldNotBeNull()
            .FilePath.ShouldEndWith(LowercaseClassGenerator.GeneratedFileName);
    }

    /// <summary>
    /// マークアップで示した診断がすべて報告されれば、検証が成功することを確認する。
    /// </summary>
    [TestMethod]
    public async Task マークアップと一致すれば検証が成功する()
    {
        var driver = new CSharpCompilerDriver()
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .AddMarkupSource("Test0.cs", "public class {|TEST0001:c|} { }");

        using var result = await driver
            .VerifyAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.AllDiagnostics.ShouldHaveSingleItem();
    }

    /// <summary>
    /// 期待した診断が報告されなかった場合、その期待値を示して検証が失敗することを確認する。
    /// </summary>
    [TestMethod]
    public async Task 期待した診断が報告されなければ検証が失敗する()
    {
        var driver = new CSharpCompilerDriver()
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .AddMarkupSource("Test0.cs", "public class {|TEST0001:C|} { }");

        var exception = await Should
            .ThrowAsync<TestVerificationException>(
                () => driver.VerifyAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);

        exception.Message.ShouldContain("報告されなかった期待値");
        exception.Message.ShouldContain(LowercaseTypeNameAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// 期待していない診断が報告された場合、その診断を示して検証が失敗することを確認する。
    /// </summary>
    [TestMethod]
    public async Task 期待していない診断が報告されれば検証が失敗する()
    {
        var driver = new CSharpCompilerDriver()
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .AddSource("Test0.cs", "public class c { }");

        var exception = await Should
            .ThrowAsync<TestVerificationException>(
                () => driver.VerifyAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);

        exception.Message.ShouldContain("期待していない診断");
        exception.Message.ShouldContain(LowercaseTypeNameAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// 期待する診断の数が報告された数より少ない場合に、余りが検出されることを確認する。
    /// 同じ ID の診断を数え違えていないかを保証する。
    /// </summary>
    [TestMethod]
    public async Task 同じIDの診断は数まで照合される()
    {
        var driver = new CSharpCompilerDriver()
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .AddMarkupSource("Test0.cs", "public class {|TEST0001:c|} { } public class d { }");

        var exception = await Should
            .ThrowAsync<TestVerificationException>(
                () => driver.VerifyAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);

        exception.Message.ShouldContain("期待していない診断");
    }

    /// <summary>
    /// 位置を持たない診断を <see cref="CSharpCompilerDriver.ExpectedDiagnostics" /> で
    /// 照合できることを確認する。マークアップでは表せないケース。
    /// </summary>
    [TestMethod]
    public async Task 位置を持たない診断を期待値として指定できる()
    {
        var driver = new CSharpCompilerDriver()
            .WithSourceGenerators(new DiagnosticReportingGenerator())
            .ExpectDiagnostic(new()
            {
                Id = DiagnosticReportingGenerator.DiagnosticId,
                Severity = DiagnosticSeverity.Warning
            })
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .VerifyAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.GeneratorDiagnostics.ShouldHaveSingleItem();
    }

    /// <summary>
    /// <see cref="CSharpCompilerDriver.CompilerDiagnostics" /> で除外した診断は
    /// 期待値に挙げる必要がないことを確認する。
    /// </summary>
    [TestMethod]
    public async Task 検証対象から除外した警告は期待値に挙げなくてよい()
    {
        // CS0169: フィールドが使用されていない。既定の Errors では検証対象に含まれない。
        var driver = new CSharpCompilerDriver()
            .AddSource("Test0.cs", "public class C { private int field; }");

        using var result = await driver
            .VerifyAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.AllDiagnostics.ShouldBeEmpty();
        result.GetCompilationDiagnostics(this._testContext.CancellationToken)
            .ShouldContain(static x => x.Id == "CS0169");
    }

    public CSharpCompilerDriverDiagnosticTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
