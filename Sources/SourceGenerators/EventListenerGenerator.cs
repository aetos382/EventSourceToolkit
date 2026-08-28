using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Aetos.EventSourceToolkit.Constants;

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
                GeneratedEventListenerAttributeFullName,
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
