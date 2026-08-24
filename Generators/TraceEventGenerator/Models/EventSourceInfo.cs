namespace Aetos.Tracing.Models;

internal sealed record ContainingTypeInfo(
    string KindKeyword,
    string Name);

internal sealed record EventSourceMethodParameterInfo(
    string FullyQualifiedTypeName,
    string Name);

internal sealed record EventMetadataInfo(
    int EventId,
    string Level);

internal sealed record EventSourceMethodInfo(
    EquatableArray<string> NamespaceSegments,
    EquatableArray<ContainingTypeInfo> ContainingTypes,
    string? AccessibilityKeyword,
    string MethodName,
    EventMetadataInfo Metadata,
    EquatableArray<EventSourceMethodParameterInfo> Parameters,
    EquatableArray<DiagnosticInfo> Diagnostics);
