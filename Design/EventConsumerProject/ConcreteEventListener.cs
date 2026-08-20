using EventProducerProject;

namespace EventConsumerProject;

public sealed partial class ConcreteEventListener :
    SampleEventListenerBase
{
    /// <inheritdoc />
    protected override void Foo(int i)
    {
        Console.WriteLine($"Foo({i})");
    }
}
