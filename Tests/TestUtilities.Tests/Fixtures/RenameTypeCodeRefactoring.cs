using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

/// <summary>指定された位置にある識別子を書き換える、テスト用の CodeRefactoring。</summary>
internal sealed class RenameTypeCodeRefactoring :
    CodeRefactoringProvider
{
    public const string NewName = "Refactored";

    public const string EquivalenceKeyName = "Rename";

    /// <inheritdoc />
    public override async Task ComputeRefactoringsAsync(
        CodeRefactoringContext context)
    {
        var document = context.Document;

        var root = await document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null)
        {
            return;
        }

        var token = root.FindToken(context.Span.Start);

        if (!token.IsKind(SyntaxKind.IdentifierToken))
        {
            return;
        }

        var newToken = SyntaxFactory.Identifier(NewName).WithTriviaFrom(token);

        context.RegisterRefactoring(
            CodeAction.Create(
                $"'{NewName}' に変更する",
                _ => Task.FromResult(document.WithSyntaxRoot(root.ReplaceToken(token, newToken))),
                EquivalenceKeyName));
    }
}
