namespace Aetos.Tracing.Diagnostics;

public static class DiagnosticIds
{
    public const string EventSourceClassMustHaveValidSignature = "TEG001";
    public const string EventSourceClassMustHaveValidEventSourceAttribute = "TEG002";
    public const string EventSourceClassMustInheritFromEventSource = "TEG003";
    public const string EventSourceMethodMustHaveValidSignature = "TEG004";
    public const string EventSourceMethodMustHaveValidAttributes = "TEG005";
    public const string EventSourceMethodShouldHaveEventAttribute = "TEG006";
}
