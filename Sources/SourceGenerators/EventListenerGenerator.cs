using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.EventSourceToolkit.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed partial class EventListenerGenerator :
    IIncrementalGenerator
{
    /// <inheritdoc/>
    void IIncrementalGenerator.Initialize(
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(PostInitialize);

        var eventListenerClassProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Aetos.EventSourceToolkit.GeneratedEventListenerAttribute",
                static (node, _) => node is ClassDeclarationSyntax,
                static (context, cancellationToken) =>
                {
                    var parser = new EventListenerParser(context.SemanticModel);

                    return parser.ParseEventListener(
                        (ClassDeclarationSyntax)context.TargetNode,
                        (INamedTypeSymbol)context.TargetSymbol,
                        context.Attributes,
                        cancellationToken);
                })
            .WithTrackingName("EventListenerInfo");

        context.RegisterSourceOutput(
            eventListenerClassProvider,
            EventListenerEmitter.EmitEventListener);
    }
}
