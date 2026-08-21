using System;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

namespace Aetos.Tracing.Tests;

[TestClass]
public sealed class EventSourceGeneratorTest
{
    [TestMethod]
    public async Task とりあえず正常系()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class TestEventSource : EventSource;
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task partial修飾子がないクラスにはTEG001()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            {|TEG001:[EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            class TestEventSource : EventSource;|}
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task file修飾子があるクラスにはTEG001()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            {|TEG001:[EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            file partial class TestEventSource : EventSource;|}
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EventSourceAttributeがないクラスにはTEG002()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            {|TEG002:[GeneratedEventSource]
            partial class TestEventSource : EventSource;|}
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EventSourceAttributeのNameがnullなクラスにはTEG002()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            {|TEG002:[EventSource(Name = null)]
            [GeneratedEventSource]
            partial class TestEventSource : EventSource;|}
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EventSourceから派生していないクラスにはTEG003()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            {|TEG003:[EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class TestEventSource;|}
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task 戻り値がvoidでないEventSourceメソッドにはTEG004()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class TestEventSource : EventSource
            {
                {|TEG004:public int Foo() => 0;|}
            }
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task partial修飾子がないEventSourceメソッドにはTEG004()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class TestEventSource : EventSource
            {
                {|TEG004:public void Foo() {}|}
            }
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task 対象外のシグネチャのメソッドでもNonEventAttributeがついていればエラーにならない()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class TestEventSource : EventSource
            {
                [NonEvent]
                public void Foo() {}
            }
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EventAttributeとNonEventAttributeが両方ついていたらTEG005()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class TestEventSource : EventSource
            {
                {|TEG005:[Event(1)]
                [NonEvent]
                public void Foo() {}|}
            }
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task staticメソッドにEventAttributeがついていたらTEG004()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class TestEventSource : EventSource
            {
                {|TEG004:[Event(1)]
                public static partial void Foo();|}
            }

            partial class TestEventSource
            {
                // コンパイルエラーを避けるためのダミーの実装本体
                public static partial void Foo() {}
            }
            """;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    public EventSourceGeneratorTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
