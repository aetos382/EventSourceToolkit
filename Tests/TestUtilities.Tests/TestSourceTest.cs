using System.Linq;

using Microsoft.CodeAnalysis.Text;

using Shouldly;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests;

[TestClass]
public sealed class TestSourceTest
{
    /// <summary>
    /// <c>{|ID:...|}</c> が、その ID と範囲を持つ期待診断に変換されることを確認する。
    /// マークアップは <see cref="TestSource.Code" /> から取り除かれる。
    /// </summary>
    [TestMethod]
    public void 名前付きの範囲は期待診断になる()
    {
        var source = TestSource.FromMarkup("Test0.cs", "class {|TEST0001:c|} { }");

        source.Code.ShouldBe("class c { }");

        var diagnostics = source.GetExpectedDiagnostics();

        diagnostics.Length.ShouldBe(1);
        diagnostics[0].Id.ShouldBe("TEST0001");
        diagnostics[0].FileName.ShouldBe("Test0.cs");
        diagnostics[0].Span.ShouldBe(new TextSpan(6, 1));
        diagnostics[0].Severity.ShouldBeNull();
        diagnostics[0].Message.ShouldBeNull();
    }

    /// <summary>
    /// 同じ ID の範囲が複数あれば、その数だけ期待診断が得られることを確認する。
    /// </summary>
    [TestMethod]
    public void 同じIDの範囲が複数あればすべて期待診断になる()
    {
        var source = TestSource.FromMarkup(
            "Test0.cs", "class {|TEST0001:c|} { } class {|TEST0001:d|} { }");

        var diagnostics = source.GetExpectedDiagnostics();

        diagnostics.Length.ShouldBe(2);
        diagnostics.Select(static x => x.Id).ShouldAllBe(static x => x == "TEST0001");
        diagnostics.Select(static x => x.Span).ShouldBe([new TextSpan(6, 1), new TextSpan(18, 1)]);
    }

    /// <summary>
    /// <c>[|...|]</c> は期待診断ではなく、CodeFix / CodeRefactoring の適用位置になることを確認する。
    /// </summary>
    [TestMethod]
    public void 名前のない範囲は適用位置になる()
    {
        var source = TestSource.FromMarkup("Test0.cs", "class [|c|] { }");

        source.Code.ShouldBe("class c { }");
        source.GetExpectedDiagnostics().ShouldBeEmpty();
        source.GetTriggerSpan().ShouldBe(new TextSpan(6, 1));
    }

    /// <summary>
    /// <c>$$</c> は長さ 0 の適用位置になることを確認する。
    /// </summary>
    [TestMethod]
    public void 位置指示子は長さ0の適用位置になる()
    {
        var source = TestSource.FromMarkup("Test0.cs", "class c$$ { }");

        source.Code.ShouldBe("class c { }");
        source.GetTriggerSpan().ShouldBe(new TextSpan(7, 0));
    }

    /// <summary>
    /// <c>[|...|]</c> と <c>$$</c> の両方があれば、範囲のほうが使われることを確認する。
    /// </summary>
    [TestMethod]
    public void 範囲と位置指示子の両方があれば範囲が優先される()
    {
        var source = TestSource.FromMarkup("Test0.cs", "class [|c|] { $$ }");

        source.GetTriggerSpan().ShouldBe(new TextSpan(6, 1));
    }

    /// <summary>
    /// 適用位置の指定がなければ <see langword="null" /> になることを確認する。
    /// </summary>
    [TestMethod]
    public void 適用位置の指定がなければnullになる()
    {
        var source = TestSource.FromMarkup("Test0.cs", "class {|TEST0001:c|} { }");

        source.GetTriggerSpan().ShouldBeNull();
    }

    /// <summary>
    /// <see cref="TestSource.FromCode" /> はマークアップを解析せず、そのままソースとして扱うことを確認する。
    /// </summary>
    [TestMethod]
    public void FromCodeはマークアップを解析しない()
    {
        const string Markup = "class {|TEST0001:c|} { }";

        var source = TestSource.FromCode("Test0.cs", Markup);

        source.Code.ShouldBe(Markup);
        source.GetExpectedDiagnostics().ShouldBeEmpty();
        source.GetTriggerSpan().ShouldBeNull();
    }
}
