using System.Diagnostics.Tracing;

namespace Aetos.Tracing.Samples;

[EventSource(Name = "Aetos-Tracing-Samples-SampleEventSource")]
[GeneratedEventSource]
public sealed partial class SampleEventSource :
    EventSource
{
}
