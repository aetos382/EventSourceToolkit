using System;
using System.Diagnostics.Tracing;

namespace Aetos.EventSourceToolkit.Tests.GeneratedEventSource;

[EventSource(Name = "Aetos-Tracing-Samples-SampleEventSource")]
[GeneratedEventSource]
public sealed partial class SampleEventSource :
    EventSource
{
    public static class Keywords
    {
        public const EventKeywords A = (EventKeywords)1;
        public const EventKeywords B = (EventKeywords)2;
        public const EventKeywords C = (EventKeywords)4;
    }

    [Event(1, Level = EventLevel.Verbose, Keywords = Keywords.A | Keywords.C)]
    public partial void Foo(
        Guid relatedActivityId,
        int p0,
        string p1,
        DateTime p2,
        byte[] p3);
}

// TODO: ジェネレーター実装ができるまでコンパイルエラーを回避するためのダミー実装
partial class SampleEventSource
{
    public partial void Foo(
        Guid relatedActivityId,
        int p0,
        string p1,
        DateTime p2,
        byte[] p3)
    {
    }
}
