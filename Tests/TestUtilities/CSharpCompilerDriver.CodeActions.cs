using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

public sealed partial class CSharpCompilerDriver
{
    /// <summary>マークアップで示した位置に CodeRefactoring を 1 回適用します。</summary>
    private async Task<Project> ApplyCodeRefactoringAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        var provider = this._codeRefactoringProvider!;

        var candidates = this.PrimaryProject.Sources
            .Select(static x => (Source: x, Span: x.GetTriggerSpan()))
            .Where(static x => x.Span is not null)
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new InvalidOperationException(
                $"CodeRefactoring を適用する位置が指定されていません。{nameof(this.AddMarkupSource)} で '[|...|]' または '$$' を含むソースを追加してください。");
        }

        if (candidates.Length > 1)
        {
            throw new InvalidOperationException(
                "CodeRefactoring を適用する位置が複数のソースに指定されています。1 箇所だけにしてください。");
        }

        var (source, span) = candidates[0];

        var document = FindDocument(project, source.FileName);

        var actions = new List<CodeAction>();

        var context = new CodeRefactoringContext(
            document, span!.Value, actions.Add, cancellationToken);

        await provider.ComputeRefactoringsAsync(context).ConfigureAwait(false);

        if (actions.Count == 0)
        {
            throw new TestVerificationException(
                $"'{source.FileName}' の {span} に対して {provider.GetType().Name} は CodeRefactoring を提供しませんでした。");
        }

        var action = this.SelectCodeAction(actions);

        return await ApplyCodeActionAsync(action, project.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>報告された診断に対して CodeFix を、ソースが変化しなくなるまで繰り返し適用します。</summary>
    private async Task<(Project Project, CSharpAnalysisResult Analysis)> ApplyCodeFixesAsync(
        Project project,
        CSharpAnalysisResult analysis,
        CancellationToken cancellationToken)
    {
        var provider = this._codeFixProvider!;

        for (var iteration = 0; ; ++iteration)
        {
            var target = FindFixableDiagnostic(project, provider, analysis);

            if (target is null)
            {
                return (project, analysis);
            }

            if (iteration >= this.MaxCodeFixIterations)
            {
                throw new TestVerificationException(
                    $"CodeFix の適用が {this.MaxCodeFixIterations} 回で収束しませんでした。まだ {ExpectedDiagnostic.Format(target)} が残っています。");
            }

            var document = FindDocument(project, target.Location.SourceTree!.FilePath);

            var actions = new List<CodeAction>();

            var context = new CodeFixContext(
                document, target, (action, _) => actions.Add(action), cancellationToken);

            await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            if (actions.Count == 0)
            {
                return (project, analysis);
            }

            var action = this.SelectCodeAction(actions);

            var fixedProject = await ApplyCodeActionAsync(
                action, project.Id, cancellationToken).ConfigureAwait(false);

            var before = await GetSourcesAsync(project, cancellationToken).ConfigureAwait(false);
            var after = await GetSourcesAsync(fixedProject, cancellationToken).ConfigureAwait(false);

            if (before.SequenceEqual(after))
            {
                return (project, analysis);
            }

            project = fixedProject;

            analysis = await this.AnalyzeAsync(project, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// CodeFix の対象とする診断を 1 件選びます。
    /// ソース上の位置が最も前にあるもの。生成されたソース上の診断は文書がないため対象外。
    /// </summary>
    private static Diagnostic? FindFixableDiagnostic(
        Project project,
        CodeFixProvider provider,
        CSharpAnalysisResult analysis)
    {
        var filePaths = project.Documents
            .Select(static x => x.FilePath)
            .ToHashSet(StringComparer.Ordinal);

        return analysis.AllDiagnostics
            .Where(x => provider.FixableDiagnosticIds.Contains(x.Id, StringComparer.Ordinal))
            .Where(x => x.Location.IsInSource && filePaths.Contains(x.Location.SourceTree!.FilePath))
            .OrderBy(static x => x.Location.SourceTree!.FilePath, StringComparer.Ordinal)
            .ThenBy(static x => x.Location.SourceSpan.Start)
            .FirstOrDefault();
    }

    private CodeAction SelectCodeAction(
        IReadOnlyList<CodeAction> actions)
    {
        var candidates = actions;

        if (this.CodeActionEquivalenceKey is { } equivalenceKey)
        {
            candidates = [.. actions.Where(x => string.Equals(
                x.EquivalenceKey, equivalenceKey, StringComparison.Ordinal))];

            if (candidates.Count == 0)
            {
                var keys = string.Join(", ", actions.Select(static x => x.EquivalenceKey ?? "(null)"));

                throw new TestVerificationException(
                    $"{nameof(CodeAction.EquivalenceKey)} が '{equivalenceKey}' の CodeAction はありませんでした。提供されたのは [{keys}] です。");
            }
        }

        if (this.CodeActionIndex >= candidates.Count)
        {
            throw new TestVerificationException(
                $"{nameof(this.CodeActionIndex)} が {this.CodeActionIndex} ですが、CodeAction は {candidates.Count} 件しかありません。");
        }

        return candidates[this.CodeActionIndex];
    }

    private static async Task<Project> ApplyCodeActionAsync(
        CodeAction action,
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        var operations = await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);

        var changes = operations.OfType<ApplyChangesOperation>().ToArray();

        if (changes.Length != 1)
        {
            throw new InvalidOperationException(
                $"CodeAction '{action.Title}' は {nameof(ApplyChangesOperation)} を 1 つだけ持つ必要がありますが、{changes.Length} 個でした。");
        }

        var project = changes[0].ChangedSolution.GetProject(projectId);

        if (project is null)
        {
            throw new InvalidOperationException(
                $"CodeAction '{action.Title}' の適用によってプロジェクトが失われました。");
        }

        return project;
    }

    private static Document FindDocument(
        Project project,
        string fileName)
    {
        var document = project.Documents.FirstOrDefault(
            x => string.Equals(x.FilePath, fileName, StringComparison.Ordinal));

        if (document is null)
        {
            var fileNames = string.Join(", ", project.Documents.Select(static x => x.Name));

            throw new InvalidOperationException(
                $"'{fileName}' に対応する文書がありません。プロジェクトにあるのは [{fileNames}] です。");
        }

        return document;
    }
}
