using System;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace Aetos.Tracing.Tests;

[TestClass]
public sealed class EventSourceParserDiagnosticsTest
{
    [TestMethod]
    public async Task とりあえず正常系()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            namespace Sample;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class TestEventSource : EventSource
            {
                [Event(1)]
                public partial void Foo(int i);
            }
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    public EventSourceParserDiagnosticsTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
