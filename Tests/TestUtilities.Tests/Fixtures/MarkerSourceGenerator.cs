using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

/// <summary>アセンブリ名を埋め込んだソースを 1 つ生成するだけの、テスト用のジェネレーター。</summary>
internal sealed class MarkerSourceGenerator :
    IIncrementalGenerator
{
    public const string PostInitializationFileName = "PostInitialization.g.cs";

    public const string GeneratedFileName = "Marker.g.cs";

    /// <inheritdoc />
    public void Initialize(
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static x =>
            x.AddSource(PostInitializationFileName, "// post initialization"));

        context.RegisterSourceOutput(
            context.CompilationProvider.Select(static (x, _) => x.AssemblyName),
            static (context, assemblyName) =>
                context.AddSource(GeneratedFileName, $"// {assemblyName}"));
    }
}
