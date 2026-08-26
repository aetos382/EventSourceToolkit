using System;
using System.Diagnostics.Tracing;

using Aetos.Tracing;

using GeneratedEventSourceTests;

namespace GeneratedEventListenerTests;

[GeneratedEventListener("Aetos-Tracing-Samples-SampleEventSource")]
internal sealed class SampleEventListener :
    SampleEventSource.ListenerBase
{
    public FooArguments? Result { get; private set; }

    /// <inheritdoc />
    protected override void Foo(EventWrittenEventArgs args, int p0, string p1, DateTime p2, byte[] p3)
    {
        this.Result = new(p0, p1, p2, p3);
    }

    public sealed record FooArguments(
        int p0,
        string p1,
        DateTime p2,
        byte[] p3);
}
