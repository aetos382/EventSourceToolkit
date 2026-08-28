using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit;

[Embedded]
public static class DiagnosticIds
{
    public const string EventSourceClassMustBePartialClass = "EST001";

    public const string EventSourceClassMustNotBeAbstract = "EST002";

    public const string EventSourceClassMustDeriveFromEventSource = "EST003";

    public const string EventSourceClassMustNotBeFileLocalClass = "EST004";

    public const string ParameterTypeNotSupported = "EST005";
}
