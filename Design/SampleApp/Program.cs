using EventProducerProject;
using EventConsumerProject;

using var listener = new ConcreteEventListener();
listener.Start();

SampleEventSource.Log.Foo(3);
SampleEventSource.Log.Bar("oops");
