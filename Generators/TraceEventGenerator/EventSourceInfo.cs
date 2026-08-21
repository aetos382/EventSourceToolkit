namespace Aetos.Tracing;

internal enum TypeKind
{
    Class,
    Struct,
    Interface,
    Record
}

internal sealed record AncestorTypeInfo(
    TypeKind Kind,
    string Name);

internal sealed record EventSourceClassInfo(
    string? Namespace,
    EquatableArray<AncestorTypeInfo> AncestorTypes,
    string TypeName,
    string SourceName);

internal sealed record EventSourceInfo(
    EventSourceClassInfo? ClassInfo,
    EquatableArray<EventSourceMethodInfo> Methods,
    EquatableArray<DiagnosticInfo> Diagnostics);
