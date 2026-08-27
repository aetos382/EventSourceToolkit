using System.Diagnostics.Tracing;

namespace EventConsumerProject.ExternalSource;

public interface IExternalEventSchema
{
    [Event(1, Level = EventLevel.Informational, Keywords = Keywords.A)]
    void Foo(int i1, int i2);

    [Event(2, Level = EventLevel.Verbose, Keywords = Keywords.B, Opcode = EventOpcode.Send)]
    void Bar(Guid relatedActivityId, string s, int i, byte[] b);

    [Event(3, Level = EventLevel.Warning, Keywords = Keywords.C)]
    void Baz();

#pragma warning disable CA1034, IDE0040
    public static class Keywords
    {
        public const EventKeywords A = (EventKeywords)1;
        public const EventKeywords B = (EventKeywords)2;
        public const EventKeywords C = (EventKeywords)4;
    }
#pragma warning restore
}
