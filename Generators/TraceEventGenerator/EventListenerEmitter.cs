using Microsoft.CodeAnalysis;

using Aetos.Tracing.Models;

namespace Aetos.Tracing;

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
