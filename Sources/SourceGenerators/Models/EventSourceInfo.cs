namespace Aetos.EventSourceToolkit.SourceGenerators.Models;

internal sealed record ContainingTypeInfo(
    string KindKeyword,
    string Name);

internal sealed record EventSourceMethodParameterInfo(
    string FullyQualifiedTypeName,
    string Name,
    bool IsEnum,
    int? FixedSize);

internal sealed record EventMetadataInfo(
    int EventId,
    string Level,
    EquatableArray<string> Keywords);

internal sealed record EventSourceMethodInfo(
    EquatableArray<string> NamespaceSegments,
    EquatableArray<ContainingTypeInfo> ContainingTypes,
    string ClassName,
    string? AccessibilityKeyword,
    string MethodName,
    EventMetadataInfo Metadata,
    EquatableArray<EventSourceMethodParameterInfo> Parameters,
    bool HasRelatedActivityIdParameter);
