using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.Tracing;

[Generator(LanguageNames.CSharp)]
public sealed partial class TraceEventGenerator :
    IIncrementalGenerator
{
    /// <inheritdoc/>
    /// <param name="context"></param>
    void IIncrementalGenerator.Initialize(
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(PostInitialize);

        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Aetos.Tracing.GeneratedEventSourceAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            ParseEventSourceClass);

        context.RegisterSourceOutput(
            provider,
            EmitEventSourceClass);
    }
}
