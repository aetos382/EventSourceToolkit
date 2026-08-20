namespace GeneratedCode;

[AttributeUsage(AttributeTargets.Class)]
public sealed class GeneratedEventListenerAttribute : Attribute
{
    public GeneratedEventListenerAttribute(
        string eventSourceName)
    {
        this.EventSourceName = eventSourceName;
    }

    public string EventSourceName { get; }
}