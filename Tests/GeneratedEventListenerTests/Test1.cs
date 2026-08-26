using System;
using System.Diagnostics.Tracing;

using GeneratedEventSourceTests;

using Shouldly;

namespace GeneratedEventListenerTests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        var source = new SampleEventSource();
        var listener = new SampleEventListener();

        listener.EnableEvents(source, EventLevel.Informational);

        var datetime = DateTime.Now;

        source.Foo(Guid.NewGuid(), 3, "hello", datetime, [1, 2, 3]);

        var (p0, p1, p2, p3) = listener.Result.ShouldNotBeNull();

        p0.ShouldBe(3);
        p1.ShouldBe("hello");
        p2.ShouldBe(datetime);
        p3.ShouldBe([1, 2, 3]);
    }

    public Test1(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
