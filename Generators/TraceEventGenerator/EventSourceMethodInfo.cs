namespace Aetos.Tracing;

internal sealed record EventSourceMethodParameterInfo(
    string FullyQualifiedTypeName,
    string Name);

internal sealed record EventSourceMethodInfo(
    string Name,
    EquatableArray<EventSourceMethodParameterInfo> Parameters);
