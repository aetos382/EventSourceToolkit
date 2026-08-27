using System.Diagnostics.Tracing;

using GeneratedCode;

namespace EventConsumerProject.ExternalSource;

[GeneratedEventListener("ExternalEvents", typeof(IExternalEventSchema))]
internal sealed partial class ExternalEventListener : EventListener
{
    partial void Foo(EventWrittenEventArgs eventData, int i1, int i2)
    {
        Console.WriteLine("Foo");
    }
}
