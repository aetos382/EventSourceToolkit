using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.Tracing;

[Generator(LanguageNames.CSharp)]
public sealed partial class EventSourceAndListenerBaseGenerator :
    IIncrementalGenerator
{
    /// <inheritdoc/>
    void IIncrementalGenerator.Initialize(
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(PostInitialize);

        var eventSourceMethodProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "System.Diagnostics.Tracing.EventAttribute",
                static (node, _) => node is MethodDeclarationSyntax,
                static (context, cancellationToken) =>
                {
                    var parser = new EventSourceParser(context.SemanticModel);

                    return parser.ParseEventSource(
                        (MethodDeclarationSyntax)context.TargetNode,
                        (IMethodSymbol)context.TargetSymbol,
                        cancellationToken);
                })
            .WithTrackingName("EventSourceInfo");

        context.RegisterSourceOutput(
            eventSourceMethodProvider,
            EventSourceEmitter.EmitEventSource);

        context.RegisterSourceOutput(
            eventSourceMethodProvider,
            EventListenerBaseEmitter.EmitEventListenerBase);
    }
}
