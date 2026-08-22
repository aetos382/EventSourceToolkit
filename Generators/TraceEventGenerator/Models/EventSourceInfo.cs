namespace Aetos.Tracing.Models;

internal enum ContainingTypeKind
{
    Unknown,
    Class,
    Struct,
    Interface,
    Record
}

internal sealed record ContainingTypeInfo(
    ContainingTypeKind Kind,
    string Name);

internal sealed record EventSourceMethodParameterInfo(
    string FullyQualifiedTypeName,
    string Name);

internal sealed record EventSourceMethodInfo(
    EquatableArray<string> NamespaceSegments,
    EquatableArray<ContainingTypeInfo> ContainingTypes,
    string MethodName,
    EquatableArray<EventSourceMethodParameterInfo> Parameters,
    EquatableArray<DiagnosticInfo> Diagnostics);
