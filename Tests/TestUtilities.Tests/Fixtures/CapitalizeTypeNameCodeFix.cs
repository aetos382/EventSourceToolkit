using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

/// <summary>
/// <see cref="LowercaseTypeNameAnalyzer" /> の診断を、名前の先頭を大文字にすることで解消する CodeFix。
/// 1 回の適用で診断が消えるため、収束する。
/// </summary>
internal sealed class CapitalizeTypeNameCodeFix :
    CodeFixProvider
{
    public const string EquivalenceKeyName = "Capitalize";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [LowercaseTypeNameAnalyzer.DiagnosticId];

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider()
    {
        return null;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(
        CodeFixContext context)
    {
        var document = context.Document;

        var root = await document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);

            if (!token.IsKind(SyntaxKind.IdentifierToken))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "名前の先頭を大文字にする",
                    _ => Task.FromResult(Capitalize(document, root, token)),
                    EquivalenceKeyName),
                diagnostic);
        }
    }

    private static Document Capitalize(
        Document document,
        SyntaxNode root,
        SyntaxToken token)
    {
        var name = token.ValueText;

        var capitalized = string.Concat(
            char.ToUpperInvariant(name[0]).ToString(),
            name.AsSpan(1));

        var newToken = SyntaxFactory.Identifier(capitalized).WithTriviaFrom(token);

        return document.WithSyntaxRoot(root.ReplaceToken(token, newToken));
    }
}
