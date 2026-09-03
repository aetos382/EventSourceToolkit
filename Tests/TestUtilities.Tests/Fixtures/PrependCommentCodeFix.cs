using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

/// <summary>
/// <see cref="LowercaseTypeNameAnalyzer" /> の診断に対して、先頭にコメントを追加するだけの CodeFix。
/// 診断が消えず、ソースは毎回変化するため、収束しない。
/// </summary>
internal sealed class PrependCommentCodeFix :
    CodeFixProvider
{
    public const string Comment = "// fixed";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [LowercaseTypeNameAnalyzer.DiagnosticId];

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider()
    {
        return null;
    }

    /// <inheritdoc />
    public override Task RegisterCodeFixesAsync(
        CodeFixContext context)
    {
        var document = context.Document;

        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "コメントを追加する",
                    ct => PrependCommentAsync(document, ct),
                    nameof(PrependCommentCodeFix)),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> PrependCommentAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        return document.WithText(text.Replace(0, 0, $"{Comment}\n"));
    }
}
