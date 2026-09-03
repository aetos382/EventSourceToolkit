using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

public sealed partial class CSharpCompilerDriver
{
    /// <summary>
    /// 追加のプロジェクトとテスト対象のプロジェクトをワークスペースに作成し、テスト対象のプロジェクトを返します。
    /// テスト対象のプロジェクトは追加のプロジェクトすべてを参照する。
    /// </summary>
    private async Task<Project> CreateSolutionAsync(
        AdhocWorkspace workspace,
        CancellationToken cancellationToken)
    {
        var projectReferences = new List<ProjectReference>(this._additionalProjects.Count);

        foreach (var state in this._additionalProjects)
        {
            var info = await CreateProjectInfoAsync(state, [], cancellationToken).ConfigureAwait(false);

            var project = workspace.AddProject(info);

            await EnsureNoSyntaxErrorsAsync(project, cancellationToken).ConfigureAwait(false);

            projectReferences.Add(new(info.Id));
        }

        var primaryInfo = await CreateProjectInfoAsync(
            this.PrimaryProject, projectReferences, cancellationToken).ConfigureAwait(false);

        var primaryProject = workspace.AddProject(primaryInfo);

        await EnsureNoSyntaxErrorsAsync(primaryProject, cancellationToken).ConfigureAwait(false);

        return primaryProject;
    }

    private static async Task<ProjectInfo> CreateProjectInfoAsync(
        CSharpProjectState state,
        IReadOnlyCollection<ProjectReference> projectReferences,
        CancellationToken cancellationToken)
    {
        if (state.Sources.Count == 0)
        {
            throw new InvalidOperationException(
                $"プロジェクト '{state.Name}' にソースが 1 つも追加されていません。");
        }

        var duplicated = state.Sources
            .GroupBy(static x => x.FileName, StringComparer.Ordinal)
            .FirstOrDefault(static x => x.Count() > 1);

        if (duplicated is not null)
        {
            throw new InvalidOperationException(
                $"プロジェクト '{state.Name}' にファイル名 '{duplicated.Key}' のソースが複数あります。");
        }

        var projectId = ProjectId.CreateNewId(state.Name);

        var documents = state.Sources
            .Select(x => DocumentInfo.Create(
                DocumentId.CreateNewId(projectId, x.FileName),
                x.FileName,
                loader: TextLoader.From(TextAndVersion.Create(
                    SourceText.From(x.Code, Encoding.UTF8),
                    VersionStamp.Create(),
                    x.FileName)),
                filePath: x.FileName))
            .ToArray();

        var references = await state.ReferenceAssemblies
            .ResolveAsync(LanguageNames.CSharp, cancellationToken)
            .ConfigureAwait(false);

        return ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            state.Name,
            state.AssemblyName,
            LanguageNames.CSharp,
            compilationOptions: state.CompilationOptions,
            parseOptions: state.ParseOptions,
            documents: documents,
            projectReferences: projectReferences,
            metadataReferences: [.. references, .. state.AdditionalReferences]);
    }

    private static async Task<ImmutableArray<(string FileName, string Text)>> GetSourcesAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<(string, string)>();

        foreach (var document in project.Documents)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            builder.Add((document.Name, text.ToString()));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// 構文エラーを含むソースは意図しないコードを検証することになるため、実行前に落とす。
    /// テスト マークアップ（<c>{|ID:...|}</c> など）を <see cref="CSharpProjectState.AddSource(string, string)" />
    /// で渡してしまった場合はここで捕まる。
    /// partial メソッドの実装がないことによる CS8795 などのセマンティック エラーは対象外。
    /// </summary>
    private static async Task EnsureNoSyntaxErrorsAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        foreach (var document in project.Documents)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            if (syntaxTree is null)
            {
                continue;
            }

            var errors = syntaxTree
                .GetDiagnostics(cancellationToken)
                .Where(static x => x.Severity == DiagnosticSeverity.Error)
                .ToArray();

            if (errors.Length == 0)
            {
                continue;
            }

            var messages = string.Join(Environment.NewLine, errors.Select(static x => x.ToString()));

            throw new InvalidOperationException(
                $"'{document.Name}' に構文エラーがあります。{Environment.NewLine}{messages}");
        }
    }
}
