namespace Aetos.Tracing;

internal sealed record EventSourceInfo(
    EquatableArray<DiagnosticInfo> Diagnostics);
