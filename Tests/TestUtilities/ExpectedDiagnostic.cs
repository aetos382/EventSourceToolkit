using System;
using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

/// <summary>
/// 期待する診断 1 件。
/// <see cref="Id" /> 以外は <see langword="null" /> のとき照合しない。
/// </summary>
public sealed record ExpectedDiagnostic
{
    /// <summary>診断 ID。</summary>
    public required string Id { get; init; }

    /// <summary>診断が報告されるソースのファイル名。</summary>
    public string? FileName { get; init; }

    /// <summary>診断が報告される範囲。</summary>
    public TextSpan? Span { get; init; }

    public DiagnosticSeverity? Severity { get; init; }

    /// <summary>診断メッセージ。書式化後の完全一致で照合する。</summary>
    public string? Message { get; init; }

    public bool Matches(
        Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        if (!string.Equals(diagnostic.Id, this.Id, StringComparison.Ordinal))
        {
            return false;
        }

        if (this.Severity is { } severity && diagnostic.Severity != severity)
        {
            return false;
        }

        if (this.Message is { } message &&
            !string.Equals(diagnostic.GetMessage(CultureInfo.InvariantCulture), message, StringComparison.Ordinal))
        {
            return false;
        }

        if (this.FileName is { } fileName &&
            !string.Equals(GetFileName(diagnostic), fileName, StringComparison.Ordinal))
        {
            return false;
        }

        if (this.Span is { } span && diagnostic.Location.SourceSpan != span)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var location = (this.FileName, this.Span) switch
        {
            (null, null) => "位置指定なし",
            (var f, null) => f,
            (null, var s) => s.ToString(),
            var (f, s) => $"{f} {s}"
        };

        return $"{this.Id} ({location})";
    }

    internal static string? GetFileName(
        Diagnostic diagnostic)
    {
        return diagnostic.Location.SourceTree?.FilePath;
    }

    /// <summary>診断を、それ自身に一致する期待値として書式化します。実際の診断の表示に使う。</summary>
    internal static string Format(
        Diagnostic diagnostic)
    {
        var fileName = GetFileName(diagnostic);

        var location = fileName is null
            ? "位置指定なし"
            : $"{fileName} {diagnostic.Location.SourceSpan}";

        return $"{diagnostic.Id} [{diagnostic.Severity}] ({location}) {diagnostic.GetMessage(CultureInfo.InvariantCulture)}";
    }
}
