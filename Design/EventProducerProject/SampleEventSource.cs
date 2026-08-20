using System.Diagnostics.Tracing;
using GeneratedCode;

namespace EventProducerProject;

[EventSource(Name = "SampleEvents")]
[GeneratedEventSource]
public sealed partial class SampleEventSource : EventSource
{
    public static readonly SampleEventSource Log = new();

    [Event(1, Level = EventLevel.Informational, Keywords = Keywords.Request)]
    public partial void Foo(int i);

    [Event(2, Level = EventLevel.Error)]
    public partial void Bar(string message);

    public static class Keywords
    {
        public const EventKeywords Request = (EventKeywords)1;
    }
}
