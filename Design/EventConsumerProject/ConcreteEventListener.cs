using EventProducerProject;

namespace EventConsumerProject;

[GeneratedEventListener("SampleEvents")]
public sealed partial class ConcreteEventListener :
    SampleEventListenerBase
{
    /// <inheritdoc />
    protected override void Foo(int i)
    {
        Console.WriteLine($"Foo({i})");
    }
}
