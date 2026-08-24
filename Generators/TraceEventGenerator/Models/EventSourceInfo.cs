using System.Diagnostics.Tracing;

namespace Aetos.Tracing.Models;

internal sealed record ContainingTypeInfo(
    string KindKeyword,
    string Name);

internal sealed record EventSourceMethodParameterInfo(
    string FullyQualifiedTypeName,
    string Name);

internal sealed record EventMetadataInfo(
    int EventId,
    string Level,
    EquatableArray<string> Keywords);

internal sealed record EventSourceMethodInfo(
    EquatableArray<string> NamespaceSegments,
    EquatableArray<ContainingTypeInfo> ContainingTypes,
    string? AccessibilityKeyword,
    string MethodName,
    EventMetadataInfo Metadata,
    EquatableArray<EventSourceMethodParameterInfo> Parameters);

internal sealed record EventSourceMethodInfoWithDiagnostics(
    EventSourceMethodInfo? MethodInfo,
    EquatableArray<DiagnosticInfo> Diagnostics)
{
    public static readonly EventSourceMethodInfoWithDiagnostics Empty = new(null, []);
}
