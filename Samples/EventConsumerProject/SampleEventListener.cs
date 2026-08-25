using System;
using System.Collections.Generic;
using System.Text;

using Aetos.Tracing.Samples.EventProducerProject;

namespace Aetos.Tracing.Samples.EventConsumerProject;

[GeneratedEventListener("Sample-EventSource")]
internal sealed class SampleEventListener :
    SampleEventSource.ListenerBase
{
    protected override void Foo(int p0, string p1, DateTime p2, byte[] p3)
    {
        Console.WriteLine($"Foo({p0}, {p1}, {p2}, {p3});");
    }
}
