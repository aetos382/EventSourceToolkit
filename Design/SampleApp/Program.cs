using EventProducerProject;
using EventConsumerProject;

using var listener = new SampleEventListener();
listener.Start();

SampleEventSource.Log.Foo(3, 4);
