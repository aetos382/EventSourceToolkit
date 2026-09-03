using System;
using System.Threading.Tasks;

using Shouldly;

using Aetos.EventSourceToolkit.SourceGenerators;
using Aetos.EventSourceToolkit.Tests.TestUtilities;

namespace Aetos.EventSourceToolkit.Tests.SourceGenerators;

[TestClass]
public sealed class EventListenerGeneratorTest
{
    /// <summary>
    /// スキーマ型が別アセンブリにある場合でも、ジェネレーターがその型を解決できることを確認する。
    /// スキーマ型は EventSourceGenerator の生成物である必要はないため、
    /// 参照される側はジェネレーターを実行しない別プロジェクトとして構成するだけでよい。
    /// </summary>
    [TestMethod]
    public async Task 別アセンブリのスキーマ型を参照できる()
    {
        var testCancellationToken = this._testContext.CancellationToken;

        /* lang=c# */
        const string EventSourceCode =
            """
            using System;
            using System.Diagnostics.Tracing;

            namespace Samples;

            public sealed class SampleEventSource : EventSource
            {
                [Event(1)]
                public void Foo() {}

                [Event(2)]
                public void Bar(int i) {}

                [Event(3)]
                public void Baz(Guid relatedActivityId) {}

                [Event(4)]
                public void Qux(Guid relatedActivityId, int i) {}
            }
            """;

        /* lang=c# */
        const string EventListenerCode =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

            using Samples;

            namespace Tests;

            [GeneratedEventListener("SampleEventSource", typeof(SampleEventSource))]
            internal sealed partial class SampleEventListener : EventListener
            {
            }
            """;

        var driver = new CSharpCompilerDriver()
            .WithSourceGenerators(new EventListenerGenerator())
            .AddProject(
                "SampleEventSourceAssembly",
                static x => x.AddSource("SampleEventSource.cs", EventSourceCode))
            .AddSource("SampleEventListener.cs", EventListenerCode);

        using var result = await driver
            .RunAsync(testCancellationToken)
            .ConfigureAwait(false);

        result.InputCompilation
            .GetTypeByMetadataName("Samples.SampleEventSource")
            .ShouldNotBeNull();

        result.GeneratorDiagnostics.ShouldBeEmpty();
    }

    public EventListenerGeneratorTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
