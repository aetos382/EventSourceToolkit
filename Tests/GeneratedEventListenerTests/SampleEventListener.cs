using System;
using System.Collections.Generic;

using Aetos.Tracing;

using GeneratedEventSourceTests;

namespace GeneratedEventListenerTests;

[GeneratedEventListener("Aetos-Tracing-Samples-SampleEventSource")]
internal sealed class SampleEventListener :
    SampleEventSource.ListenerBase
{
    private readonly Queue<FooArguments> _arguments;

    public SampleEventListener(
        Queue<FooArguments> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        this._arguments = arguments;
    }

    protected override void Foo(int p0, string p1, DateTime p2, byte[] p3)
    {
        this._arguments.Enqueue(new(p0, p1, p2, p3));
    }

    public sealed record FooArguments(
        int p0,
        string p1,
        DateTime p2,
        byte[] p3);
}
