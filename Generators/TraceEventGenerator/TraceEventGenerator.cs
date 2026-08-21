using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.Tracing;

[Generator(LanguageNames.CSharp)]
public sealed partial class TraceEventGenerator :
    IIncrementalGenerator
{
    /// <inheritdoc/>
    void IIncrementalGenerator.Initialize(
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(PostInitialize);

        var eventSourceInfoProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Aetos.Tracing.GeneratedEventSourceAttribute",
                static (node, _) => node is ClassDeclarationSyntax,
                static (context, cancellationToken) => ParseEventSourceClass(
                    context.SemanticModel,
                    (ClassDeclarationSyntax)context.TargetNode,
                    (INamedTypeSymbol)context.TargetSymbol,
                    cancellationToken))
            .WithTrackingName("EventSourceInfo");

        context.RegisterSourceOutput(
            eventSourceInfoProvider,
            EmitEventSourceClass);
    }
}
