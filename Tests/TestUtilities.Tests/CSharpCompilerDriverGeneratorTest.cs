using System;
using System.Linq;
using System.Threading.Tasks;

using Shouldly;

using Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests;

[TestClass]
public sealed class CSharpCompilerDriverGeneratorTest
{
    /// <summary>
    /// ジェネレーターが生成したソースが、PostInitialize のものを含めてすべて取得できることを確認する。
    /// </summary>
    [TestMethod]
    public async Task 生成されたソースを取得できる()
    {
        var driver = new CSharpCompilerDriver
        {
            AssemblyName = "GeneratorTest"
        };

        driver
            .WithSourceGenerators(new MarkerSourceGenerator())
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.GeneratedFileNames.ShouldBe(
            [MarkerSourceGenerator.PostInitializationFileName, MarkerSourceGenerator.GeneratedFileName],
            ignoreOrder: true);

        result.GetGeneratedText(MarkerSourceGenerator.GeneratedFileName)
            .ShouldBe("// GeneratorTest");
    }

    /// <summary>生成されたソースが、生成後のコンパイルに含まれていることを確認する。</summary>
    [TestMethod]
    public async Task 生成されたソースは生成後のコンパイルに含まれる()
    {
        var driver = new CSharpCompilerDriver()
            .WithSourceGenerators(new MarkerSourceGenerator())
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.InputCompilation.SyntaxTrees.Count().ShouldBe(1);
        result.OutputCompilation.SyntaxTrees.Count().ShouldBe(3);
        result.CompilerDiagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// 生成されていないファイル名を要求した場合、生成されたファイル名を含む例外になることを確認する。
    /// </summary>
    [TestMethod]
    public async Task 生成されていないソースを要求すると例外になる()
    {
        var driver = new CSharpCompilerDriver()
            .WithSourceGenerators(new MarkerSourceGenerator())
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.FindGeneratedSource("Missing.g.cs").ShouldBeNull();

        var exception = Should.Throw<InvalidOperationException>(
            () => result.GetGeneratedText("Missing.g.cs"));

        exception.Message.ShouldContain(MarkerSourceGenerator.GeneratedFileName);
    }

    /// <summary>
    /// インクリメンタル ステップの検証ができるよう、ドライバーの生の実行結果が取得できることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ドライバーの実行結果を取得できる()
    {
        var driver = new CSharpCompilerDriver()
            .WithSourceGenerators(new MarkerSourceGenerator())
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        var runResult = result.GeneratorRunResult.ShouldNotBeNull();

        runResult.Results.Length.ShouldBe(1);
        runResult.Results[0].TrackedSteps.ShouldNotBeEmpty();
    }

    /// <summary>ジェネレーターが報告した診断が取得できることを確認する。</summary>
    [TestMethod]
    public async Task ジェネレーターの診断を取得できる()
    {
        var driver = new CSharpCompilerDriver()
            .WithSourceGenerators(new DiagnosticReportingGenerator())
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.GeneratorDiagnostics.ShouldHaveSingleItem()
            .Id.ShouldBe(DiagnosticReportingGenerator.DiagnosticId);

        result.AllDiagnostics.ShouldContain(
            static x => x.Id == DiagnosticReportingGenerator.DiagnosticId);
    }

    /// <summary>
    /// 複数のジェネレーターを同時に実行でき、それぞれの生成物が得られることを確認する。
    /// </summary>
    [TestMethod]
    public async Task 複数のジェネレーターを実行できる()
    {
        var driver = new CSharpCompilerDriver()
            .WithSourceGenerators(new MarkerSourceGenerator(), new DiagnosticReportingGenerator())
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.GeneratedFileNames.ShouldContain(MarkerSourceGenerator.GeneratedFileName);

        result.GeneratorDiagnostics.ShouldHaveSingleItem()
            .Id.ShouldBe(DiagnosticReportingGenerator.DiagnosticId);
    }

    public CSharpCompilerDriverGeneratorTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
