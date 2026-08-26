using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Aetos.Tracing.Constants;

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

        GenerateEventSourceAndListenerBase(context);
        GenerateDerivedEventListener(context);
    }

    private static void GenerateEventSourceAndListenerBase(
        IncrementalGeneratorInitializationContext context)
    {
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

    private static void GenerateDerivedEventListener(
        IncrementalGeneratorInitializationContext context)
    {
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
