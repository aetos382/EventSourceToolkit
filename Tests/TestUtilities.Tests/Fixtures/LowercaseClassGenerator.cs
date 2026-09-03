using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

/// <summary>
/// <see cref="LowercaseTypeNameAnalyzer" /> の診断を誘発するソースを生成する、テスト用のジェネレーター。
/// Analyzer が生成後のコンパイルに対して実行されることを確認するために使う。
/// 生成コードとして扱われて解析から除外されないよう、ファイル名は <c>.g.cs</c> にしない。
/// </summary>
internal sealed class LowercaseClassGenerator :
    IIncrementalGenerator
{
    public const string GeneratedFileName = "LowercaseClass.cs";

    /// <inheritdoc />
    public void Initialize(
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static x =>
            x.AddSource(GeneratedFileName, "public class generated { }"));
    }
}
