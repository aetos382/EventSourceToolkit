using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

using GeneratedEventSourceTests;

using static GeneratedEventListenerTests.SampleEventListener;

namespace GeneratedEventListenerTests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        var queue = new Queue<FooArguments>();
        var source = new SampleEventSource();
        var listener = new SampleEventListener(queue);

        listener.EnableEvents(source, EventLevel.Informational);

        var datetime = DateTime.Now;

        source.Foo(Guid.NewGuid(), 3, "hello", datetime, [1, 2, 3]);

        Assert.ContainsSingle(queue);
    }

    public Test1(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
