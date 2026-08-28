using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Aetos.EventSourceToolkit.Analyzers;
using Aetos.EventSourceToolkit.SourceGenerators;

namespace Aetos.EventSourceToolkit.Tests.Analyzers;

[TestClass]
public sealed class EventSourceClassSignatureAnalyzerTest
{
    private sealed class Test : CSharpAnalyzerTest<EventSourceClassSignatureAnalyzer, DefaultVerifier>
    {
        /// <inheritdoc />
        protected override IEnumerable<Type> GetSourceGenerators()
        {
            yield return typeof(EventSourceGenerator);
        }
    }

    [TestMethod]
    public async Task 正常系()
    {
        /* lang=c# */
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

            [GeneratedEventSource]
            public sealed partial class MyEventSource : EventSource
            {
            }
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var test = new Test
        {
            TestCode = Code
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task イベントソースクラスがpartialでない場合はEST001が出る()
    {
        /* lang=c#-test */
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

            [GeneratedEventSource]
            public sealed class {|EST001:MyEventSource|} : EventSource
            {
            }
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var test = new Test
        {
            TestCode = Code
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task イベントソースクラスを包含する型がpartialでない場合はEST001が出る()
    {
        /* lang=c#-test */
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

            public static class {|EST001:Outer|}
            {
                [GeneratedEventSource]
                public sealed partial class MyEventSource : EventSource
                {
                }
            }
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var test = new Test
        {
            TestCode = Code
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task イベントソースクラスがabstractの場合はEST002が出る()
    {
        /* lang=c#-test */
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

            [GeneratedEventSource]
            public abstract partial class {|EST002:MyEventSource|} : EventSource
            {
            }
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var test = new Test
        {
            TestCode = Code
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GeneratedEventSourceがついていないpartialパーツがabstractでもEST002が出る()
    {
        /* lang=c#-test */
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

            [GeneratedEventSource]
            public partial class MyEventSource : EventSource
            {
            }

            abstract partial class {|EST002:MyEventSource|};
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var test = new Test
        {
            TestCode = Code
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task イベントソースクラスがEventSourceから派生していない場合はEST003が出る()
    {
        /* lang=c#-test */
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

            [GeneratedEventSource]
            public partial class {|EST003:MyEventSource|}
            {
            }
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var test = new Test
        {
            TestCode = Code
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task イベントソースクラスのpartialパーツのいずれかがEventSourceから派生していればEST003は出ない()
    {
        /* lang=c# */
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

            [GeneratedEventSource]
            public partial class MyEventSource
            {
            }

            partial class MyEventSource : EventSource;
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var test = new Test
        {
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task イベントソースクラスがfileローカルクラスの場合はEST004が出る()
    {
        /* lang=c#-test */
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

            [GeneratedEventSource]
            file sealed partial class {|EST004:MyEventSource|} : EventSource
            {
            }
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var test = new Test
        {
            TestCode = Code
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }



    [TestMethod]
    public async Task イベントソースクラスを包含する型がfileローカルクラスの場合はEST004が出る()
    {
        /* lang=c#-test */
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.EventSourceToolkit;

            file static partial class {|EST004:Outer|}
            {
                [GeneratedEventSource]
                public sealed partial class MyEventSource : EventSource
                {
                }
            }
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var test = new Test
        {
            TestCode = Code
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }

    public EventSourceClassSignatureAnalyzerTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
