using System;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using Aetos.EventSourceToolkit.Tests.TestUtilities;

namespace Aetos.EventSourceToolkit.Tests.Analyzers;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public async Task 正常系()
    {
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

        var analyzerConfigOptionsProvider = new TestAnalyzerConfigOptionsProvider();

        var parseOptions = CSharpParseOptions.Default;

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: true,
            nullableContextOptions: NullableContextOptions.Enable);

        var analysisOptions = new CompilationWithAnalyzersOptions(
            new(
                [],
                analyzerConfigOptionsProvider),
            static (e, a, d) => { },
            true,
            true,
            false,
            static (e) => true,
            (a) => analyzerConfigOptionsProvider);
    }

    public Test1(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
