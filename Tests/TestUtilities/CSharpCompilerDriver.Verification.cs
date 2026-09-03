using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

public sealed partial class CSharpCompilerDriver
{
    /// <summary>
    /// マークアップおよび <see cref="ExpectedDiagnostics" /> から、期待するすべての診断を返します。
    /// </summary>
    public ImmutableArray<ExpectedDiagnostic> GetAllExpectedDiagnostics()
    {
        return
        [
            .. this.PrimaryProject.Sources.SelectMany(static x => x.GetExpectedDiagnostics()),
            .. this.ExpectedDiagnostics
        ];
    }

    /// <summary>
    /// 報告された診断が期待どおりであることを確認します。
    /// 期待値は先頭から順に、最初に一致した診断と対応付ける。
    /// </summary>
    private void VerifyDiagnostics(
        CSharpCompilerResult result)
    {
        var remaining = result.AllDiagnostics.ToList();
        var missing = new List<ExpectedDiagnostic>();

        foreach (var expected in this.GetAllExpectedDiagnostics())
        {
            var index = remaining.FindIndex(expected.Matches);

            if (index < 0)
            {
                missing.Add(expected);
            }
            else
            {
                remaining.RemoveAt(index);
            }
        }

        if (missing.Count == 0 && remaining.Count == 0)
        {
            return;
        }

        var message = new StringBuilder("診断が期待どおりではありません。");

        if (missing.Count > 0)
        {
            message.AppendLine().Append("報告されなかった期待値:");

            foreach (var expected in missing)
            {
                message.AppendLine().Append("  ").Append(expected);
            }
        }

        if (remaining.Count > 0)
        {
            message.AppendLine().Append("期待していない診断:");

            foreach (var diagnostic in remaining)
            {
                message.AppendLine().Append("  ").Append(ExpectedDiagnostic.Format(diagnostic));
            }
        }

        throw new TestVerificationException(message.ToString());
    }

    /// <summary>
    /// <see cref="ExpectedFixedSources" /> に指定されたソースが期待どおりであることを確認します。
    /// 改行の違いは無視する。
    /// </summary>
    private void VerifyFixedSources(
        CSharpCompilerResult result)
    {
        foreach (var (fileName, expected) in this.ExpectedFixedSources)
        {
            var actual = result.FinalSources
                .Where(x => string.Equals(x.FileName, fileName, StringComparison.Ordinal))
                .Select(static x => x.Text)
                .FirstOrDefault();

            if (actual is null)
            {
                var fileNames = string.Join(", ", result.FinalSources.Select(static x => x.FileName));

                throw new TestVerificationException(
                    $"'{fileName}' がプロジェクトにありません。あるのは [{fileNames}] です。");
            }

            if (!string.Equals(Normalize(expected), Normalize(actual), StringComparison.Ordinal))
            {
                throw new TestVerificationException(
                    $"'{fileName}' が期待どおりではありません。{Environment.NewLine}期待値:{Environment.NewLine}{expected}{Environment.NewLine}実際:{Environment.NewLine}{actual}");
            }
        }
    }

    private static string Normalize(
        string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
