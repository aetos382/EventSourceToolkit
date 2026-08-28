using System;
using System.Threading.Tasks;

using Aetos.EventSourceToolkit.SourceGenerators;

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Aetos.EventSourceToolkit.Tests.SourceGenerators;

[TestClass]
public sealed class EventSourceParserDiagnosticsTest
{
    private sealed class Test : CSharpSourceGeneratorTest<EventSourceGenerator, DefaultVerifier>;

    [TestMethod]
    public async Task とりあえず正常系()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

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
