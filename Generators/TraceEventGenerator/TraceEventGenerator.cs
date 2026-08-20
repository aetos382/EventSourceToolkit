using Microsoft.CodeAnalysis;

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
    }
}
