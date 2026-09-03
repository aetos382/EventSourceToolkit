using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using Shouldly;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests;

[TestClass]
public sealed class ExpectedDiagnosticTest
{
    private static readonly DiagnosticDescriptor Descriptor = new(
        "TESTX001",
        "タイトル",
        "メッセージ {0}",
        "Test",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly TextSpan Span = new(6, 1);

    /// <summary>
    /// ID だけを指定した期待値が、位置やメッセージに関係なく一致することを確認する。
    /// 指定していない項目は照合しないという既定の挙動。
    /// </summary>
    [TestMethod]
    public void ID以外を指定しなければIDだけで一致する()
    {
        var expected = new ExpectedDiagnostic
        {
            Id = "TESTX001"
        };

        expected.Matches(CreateDiagnostic()).ShouldBeTrue();
    }

    /// <summary>ID が異なれば一致しないことを確認する。</summary>
    [TestMethod]
    public void IDが異なれば一致しない()
    {
        var expected = new ExpectedDiagnostic
        {
            Id = "TESTX002"
        };

        expected.Matches(CreateDiagnostic()).ShouldBeFalse();
    }

    /// <summary>指定した範囲が診断の範囲と一致するかを照合することを確認する。</summary>
    [TestMethod]
    public void 範囲を指定すれば範囲まで照合する()
    {
        var matching = new ExpectedDiagnostic
        {
            Id = "TESTX001",
            Span = Span
        };

        var notMatching = matching with
        {
            Span = new TextSpan(0, 1)
        };

        matching.Matches(CreateDiagnostic()).ShouldBeTrue();
        notMatching.Matches(CreateDiagnostic()).ShouldBeFalse();
    }

    /// <summary>指定したファイル名が診断の報告先と一致するかを照合することを確認する。</summary>
    [TestMethod]
    public void ファイル名を指定すればファイル名まで照合する()
    {
        var matching = new ExpectedDiagnostic
        {
            Id = "TESTX001",
            FileName = "Test0.cs"
        };

        var notMatching = matching with
        {
            FileName = "Other.cs"
        };

        matching.Matches(CreateDiagnostic()).ShouldBeTrue();
        notMatching.Matches(CreateDiagnostic()).ShouldBeFalse();
    }

    /// <summary>指定した重大度が診断の重大度と一致するかを照合することを確認する。</summary>
    [TestMethod]
    public void 重大度を指定すれば重大度まで照合する()
    {
        var matching = new ExpectedDiagnostic
        {
            Id = "TESTX001",
            Severity = DiagnosticSeverity.Warning
        };

        var notMatching = matching with
        {
            Severity = DiagnosticSeverity.Error
        };

        matching.Matches(CreateDiagnostic()).ShouldBeTrue();
        notMatching.Matches(CreateDiagnostic()).ShouldBeFalse();
    }

    /// <summary>指定したメッセージが、書式化後の診断メッセージと完全一致で照合されることを確認する。</summary>
    [TestMethod]
    public void メッセージを指定すれば書式化後の文字列で照合する()
    {
        var matching = new ExpectedDiagnostic
        {
            Id = "TESTX001",
            Message = "メッセージ 引数"
        };

        var notMatching = matching with
        {
            Message = "メッセージ"
        };

        matching.Matches(CreateDiagnostic()).ShouldBeTrue();
        notMatching.Matches(CreateDiagnostic()).ShouldBeFalse();
    }

    private static Diagnostic CreateDiagnostic()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("class c { }", path: "Test0.cs");

        return Diagnostic.Create(Descriptor, Location.Create(syntaxTree, Span), "引数");
    }
}
