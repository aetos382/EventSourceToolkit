using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

/// <summary>
/// コンパイル対象のソース 1 件。
/// <see cref="FromMarkup" /> で作成した場合はテスト マークアップを解析し、
/// <see cref="Code" /> からは取り除く。
/// </summary>
/// <param name="FileName">ファイル名。診断の位置の照合に使う。</param>
/// <param name="Code">マークアップを取り除いたソース。</param>
/// <param name="Positions"><c>$$</c> の位置。</param>
/// <param name="Spans">
/// マークアップの範囲。キーが空文字列のものは <c>[|...|]</c>、それ以外は <c>{|ID:...|}</c> の ID。
/// </param>
public sealed record TestSource(
    string FileName,
    string Code,
    ImmutableArray<int> Positions,
    ImmutableDictionary<string, ImmutableArray<TextSpan>> Spans)
{
    /// <summary>マークアップを解析せず、そのままソースとして扱います。</summary>
    public static TestSource FromCode(
        string fileName,
        string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentNullException.ThrowIfNull(code);

        return new(fileName, code, [], []);
    }

    /// <summary>テスト マークアップを解析します。</summary>
    public static TestSource FromMarkup(
        string fileName,
        string markup)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentNullException.ThrowIfNull(markup);

        TestFileMarkupParser.GetPositionsAndSpans(
            markup, out var code, out var positions, out var spans);

        return new(fileName, code, positions, spans);
    }

    /// <summary>
    /// <c>{|ID:...|}</c> から導出した、期待する診断。
    /// </summary>
    public ImmutableArray<ExpectedDiagnostic> GetExpectedDiagnostics()
    {
        var builder = ImmutableArray.CreateBuilder<ExpectedDiagnostic>();

        foreach (var (id, spans) in this.Spans)
        {
            if (id.Length == 0)
            {
                continue;
            }

            foreach (var span in spans)
            {
                builder.Add(new()
                {
                    Id = id,
                    FileName = this.FileName,
                    Span = span
                });
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// CodeFix / CodeRefactoring を適用する位置。
    /// <c>[|...|]</c> があればその範囲、なければ <c>$$</c> の位置。どちらもなければ <see langword="null" />。
    /// </summary>
    public TextSpan? GetTriggerSpan()
    {
        if (this.Spans.TryGetValue(string.Empty, out var spans) && spans.Length > 0)
        {
            return spans[0];
        }

        if (this.Positions.Length > 0)
        {
            return new(this.Positions[0], 0);
        }

        return null;
    }
}
