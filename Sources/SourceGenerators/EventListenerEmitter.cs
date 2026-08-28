using Microsoft.CodeAnalysis;

using Aetos.EventSourceToolkit.SourceGenerators.Models;

namespace Aetos.EventSourceToolkit.SourceGenerators;

internal static class EventListenerEmitter
{
    public static void EmitEventListener(
        SourceProductionContext context,
        EventListenerInfoWithDiagnostics input)
    {
        foreach (var diagnostic in input.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.CreateDiagnostic());
        }

        if (input.ListenerInfo is not { } listenerInfo)
        {
            return;
        }
    }
}
