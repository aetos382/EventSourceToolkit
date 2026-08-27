using System.Diagnostics.Tracing;

using EventProducerProject;

using GeneratedCode;

namespace EventConsumerProject;

[GeneratedEventListener("SampleEvents", typeof(SampleEventSource))]
public sealed partial class SampleEventListener : EventListener
{
    partial void Foo(EventWrittenEventArgs eventData, int i1, int i2)
    {
        Console.WriteLine("Foo");
    }
}
