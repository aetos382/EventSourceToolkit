namespace Aetos.Tracing.Models;

internal sealed record EventListenerInfo;

internal sealed record EventListenerInfoWithDiagnostics(
    EventListenerInfo? ListenerInfo,
    EquatableArray<DiagnosticInfo> Diagnostics)
{
    public static readonly EventListenerInfoWithDiagnostics Empty = new(null, []);
}
