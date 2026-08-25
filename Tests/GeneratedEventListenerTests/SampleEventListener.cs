using System;

using Aetos.Tracing;

using GeneratedEventSourceTests;

namespace GeneratedEventListenerTests;

[GeneratedEventListener("Aetos-Tracing-Samples-SampleEventSource")]
internal sealed class SampleEventListener :
    SampleEventSource.ListenerBase
{
    protected override void Foo(int p0, string p1, DateTime p2, byte[] p3)
    {
        Console.WriteLine($"Foo({p0}, {p1}, {p2}, {p3});");
    }
}
