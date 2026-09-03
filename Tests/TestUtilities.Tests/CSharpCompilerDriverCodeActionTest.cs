using System;
using System.Threading.Tasks;

using Shouldly;

using Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests;

[TestClass]
public sealed class CSharpCompilerDriverCodeActionTest
{
    /// <summary>
    /// 報告された診断に CodeFix が適用され、期待するソースになることを確認する。
    /// 適用後は診断が消えるため、マークアップの期待値は元のソースに書く。
    /// </summary>
    [TestMethod]
    public async Task CodeFixが適用される()
    {
        var driver = new CSharpCompilerDriver()
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .WithCodeFix(new CapitalizeTypeNameCodeFix())
            .AddMarkupSource("Test0.cs", "public class {|TEST0001:c|} { }")
            .ExpectFixedSource("Test0.cs", "public class C { }");

        using var result = await driver
            .VerifyAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.RemainingDiagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// 診断が複数ある場合でも、CodeFix が繰り返し適用されてすべて解消されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task CodeFixが繰り返し適用される()
    {
        var driver = new CSharpCompilerDriver()
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .WithCodeFix(new CapitalizeTypeNameCodeFix())
            .AddMarkupSource(
                "Test0.cs",
                "public class {|TEST0001:c|} { } public class {|TEST0001:d|} { }")
            .ExpectFixedSource("Test0.cs", "public class C { } public class D { }");

        using var result = await driver
            .VerifyAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.RemainingDiagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// 検証の対象となる診断は CodeFix の適用前のソースに対するものであり、
    /// 適用後に残った診断は別に取得できることを確認する。
    /// マークアップの期待値を適用前のソースに書けばよいことの根拠。
    /// </summary>
    [TestMethod]
    public async Task 診断は適用前と適用後の両方が取得できる()
    {
        var driver = new CSharpCompilerDriver()
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .WithCodeFix(new CapitalizeTypeNameCodeFix())
            .AddSource("Test0.cs", "public class c { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.AllDiagnostics.ShouldHaveSingleItem()
            .Id.ShouldBe(LowercaseTypeNameAnalyzer.DiagnosticId);

        result.RemainingDiagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// 診断を解消しない CodeFix を指定した場合、上限まで繰り返した後に失敗することを確認する。
    /// 無限ループにならないことの保証。
    /// </summary>
    [TestMethod]
    public async Task 収束しないCodeFixは上限で失敗する()
    {
        var driver = new CSharpCompilerDriver
        {
            MaxCodeFixIterations = 3
        };

        driver
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .WithCodeFix(new PrependCommentCodeFix())
            .AddSource("Test0.cs", "public class c { }");

        var exception = await Should
            .ThrowAsync<TestVerificationException>(
                () => driver.RunAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);

        exception.Message.ShouldContain("3");
        exception.Message.ShouldContain(LowercaseTypeNameAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// ソースが変化しなくなった時点で CodeFix の適用が止まることを確認する。
    /// 診断が残っていても、変化がなければ失敗ではない。
    /// </summary>
    [TestMethod]
    public async Task CodeFixが対象としない診断は残る()
    {
        var driver = new CSharpCompilerDriver()
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .WithCodeFix(new CapitalizeTypeNameCodeFix())
            .AddSource("Test0.cs", "public class C { private int field; }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.FinalSources.ShouldHaveSingleItem()
            .Text.ShouldBe("public class C { private int field; }");
    }

    /// <summary>
    /// <see cref="CSharpCompilerDriver.CodeActionEquivalenceKey" /> で、
    /// 適用する CodeAction を選択できることを確認する。
    /// </summary>
    [TestMethod]
    public async Task EquivalenceKeyでCodeActionを選択できる()
    {
        var driver = new CSharpCompilerDriver
        {
            CodeActionEquivalenceKey = CapitalizeTypeNameCodeFix.EquivalenceKeyName
        };

        driver
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .WithCodeFix(new CapitalizeTypeNameCodeFix())
            .AddSource("Test0.cs", "public class c { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.FinalSources.ShouldHaveSingleItem().Text.ShouldBe("public class C { }");
    }

    /// <summary>
    /// 一致する <see cref="CSharpCompilerDriver.CodeActionEquivalenceKey" /> がない場合、
    /// 提供されたキーを示して失敗することを確認する。
    /// </summary>
    [TestMethod]
    public async Task 一致するEquivalenceKeyがなければ失敗する()
    {
        var driver = new CSharpCompilerDriver
        {
            CodeActionEquivalenceKey = "存在しないキー"
        };

        driver
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .WithCodeFix(new CapitalizeTypeNameCodeFix())
            .AddSource("Test0.cs", "public class c { }");

        var exception = await Should
            .ThrowAsync<TestVerificationException>(
                () => driver.RunAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);

        exception.Message.ShouldContain(CapitalizeTypeNameCodeFix.EquivalenceKeyName);
    }

    /// <summary>
    /// <see cref="CSharpCompilerDriver.CodeActionIndex" /> が範囲外の場合に失敗することを確認する。
    /// </summary>
    [TestMethod]
    public async Task CodeActionIndexが範囲外なら失敗する()
    {
        var driver = new CSharpCompilerDriver
        {
            CodeActionIndex = 1
        };

        driver
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .WithCodeFix(new CapitalizeTypeNameCodeFix())
            .AddSource("Test0.cs", "public class c { }");

        await Should
            .ThrowAsync<TestVerificationException>(
                () => driver.RunAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// <c>[|...|]</c> で示した範囲に CodeRefactoring が適用されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task 範囲で示した位置にCodeRefactoringが適用される()
    {
        var driver = new CSharpCompilerDriver()
            .WithCodeRefactoring(new RenameTypeCodeRefactoring())
            .AddMarkupSource("Test0.cs", "public class [|Target|] { }")
            .ExpectFixedSource("Test0.cs", $"public class {RenameTypeCodeRefactoring.NewName} {{ }}");

        using var result = await driver
            .VerifyAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.RemainingDiagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// <c>$$</c> で示した位置に CodeRefactoring が適用されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task 位置指示子で示した位置にCodeRefactoringが適用される()
    {
        var driver = new CSharpCompilerDriver()
            .WithCodeRefactoring(new RenameTypeCodeRefactoring())
            .AddMarkupSource("Test0.cs", "public class Tar$$get { }")
            .ExpectFixedSource("Test0.cs", $"public class {RenameTypeCodeRefactoring.NewName} {{ }}");

        using var result = await driver
            .VerifyAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.RemainingDiagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// 適用位置を示していない場合、セットアップの誤りとして例外になることを確認する。
    /// </summary>
    [TestMethod]
    public async Task CodeRefactoringの適用位置がなければ例外になる()
    {
        var driver = new CSharpCompilerDriver()
            .WithCodeRefactoring(new RenameTypeCodeRefactoring())
            .AddSource("Test0.cs", "public class Target { }");

        await Should
            .ThrowAsync<InvalidOperationException>(
                () => driver.RunAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// CodeRefactoring が提供されなかった場合、検証の失敗として扱われることを確認する。
    /// </summary>
    [TestMethod]
    public async Task CodeRefactoringが提供されなければ失敗する()
    {
        var driver = new CSharpCompilerDriver()
            .WithCodeRefactoring(new RenameTypeCodeRefactoring())
            .AddMarkupSource("Test0.cs", "public class Target { [|/* コメント */|] }");

        await Should
            .ThrowAsync<TestVerificationException>(
                () => driver.RunAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 期待する修正後のソースと一致しない場合、両方を示して検証が失敗することを確認する。
    /// </summary>
    [TestMethod]
    public async Task 修正後のソースが一致しなければ検証が失敗する()
    {
        var driver = new CSharpCompilerDriver()
            .WithCodeRefactoring(new RenameTypeCodeRefactoring())
            .AddMarkupSource("Test0.cs", "public class [|Target|] { }")
            .ExpectFixedSource("Test0.cs", "public class Other { }");

        var exception = await Should
            .ThrowAsync<TestVerificationException>(
                () => driver.VerifyAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);

        exception.Message.ShouldContain("public class Other { }");
        exception.Message.ShouldContain($"public class {RenameTypeCodeRefactoring.NewName} {{ }}");
    }

    /// <summary>
    /// 存在しないファイル名を期待値に指定した場合、セットアップの誤りとして失敗することを確認する。
    /// </summary>
    [TestMethod]
    public async Task 存在しないファイルを期待値に指定すれば失敗する()
    {
        var driver = new CSharpCompilerDriver()
            .AddSource("Test0.cs", "public class C { }")
            .ExpectFixedSource("Missing.cs", "public class C { }");

        var exception = await Should
            .ThrowAsync<TestVerificationException>(
                () => driver.VerifyAsync(this._testContext.CancellationToken))
            .ConfigureAwait(false);

        exception.Message.ShouldContain("Missing.cs");
        exception.Message.ShouldContain("Test0.cs");
    }

    /// <summary>
    /// 生成されたソースは CodeFix の対象にならないことを確認する。
    /// 生成物には文書がないため、書き換えても意味がない。
    /// </summary>
    [TestMethod]
    public async Task 生成されたソースの診断はCodeFixの対象にならない()
    {
        var driver = new CSharpCompilerDriver()
            .WithSourceGenerators(new LowercaseClassGenerator())
            .WithAnalyzers(new LowercaseTypeNameAnalyzer())
            .WithCodeFix(new CapitalizeTypeNameCodeFix())
            .AddSource("Test0.cs", "public class C { }");

        using var result = await driver
            .RunAsync(this._testContext.CancellationToken)
            .ConfigureAwait(false);

        result.FinalSources.ShouldHaveSingleItem().Text.ShouldBe("public class C { }");

        result.AnalyzerDiagnostics.ShouldHaveSingleItem()
            .Id.ShouldBe(LowercaseTypeNameAnalyzer.DiagnosticId);
    }

    public CSharpCompilerDriverCodeActionTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
