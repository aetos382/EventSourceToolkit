using Microsoft.CodeAnalysis;

using Aetos.EventSourceToolkit.SourceGenerators.Models;

namespace Aetos.EventSourceToolkit.SourceGenerators;

internal static class EventListenerEmitter
{
    public static void EmitEventListener(
        SourceProductionContext context,
        EventListenerInfo? listenerInfo)
    {
        if (listenerInfo is null)
        {
            return;
        }
    }
}
