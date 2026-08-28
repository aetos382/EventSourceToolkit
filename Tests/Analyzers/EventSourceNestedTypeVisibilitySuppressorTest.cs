using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeQuality.Analyzers.ApiDesignGuidelines;

using Aetos.EventSourceToolkit.Analyzers;
using Aetos.EventSourceToolkit.Tests.TestUtilities;

namespace Aetos.EventSourceToolkit.Tests.Analyzers;

[TestClass]
public sealed class EventSourceNestedTypeVisibilitySuppressorTest
{
    private sealed class Test : CSharpAnalyzerTest
    {
        /// <inheritdoc />
        protected override CompilationOptions CreateCompilationOptions()
        {
            var options = base
                .CreateCompilationOptions()
                .WithSpecificDiagnosticOptions([
                    new("CA1034", ReportDiagnostic.Warn)
                ]);

            return options;
        }

        /// <inheritdoc />
        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            yield return new NestedTypesShouldNotBeVisibleAnalyzer();
            yield return new EventSourceNestedTypeVisibilitySuppressor();
        }
    }

    [TestMethod]
    public async Task 入れ子クラスに関する特定の警告が抑制される()
    {
        /* lang=c#-test */
        const string Code =
            """
            using System.Diagnostics.Tracing;

            public class MyEventSource : EventSource
            {
                public static class {|#0:Keywords|}
                {
                }

                public static class {|#1:Tasks|}
                {
                }

                public static class {|#2:Opcodes|}
                {
                }

                // こいつは抑制されない
                public static class {|#3:NotSuppressed|}
                {
                }
            }
            """;

        var test = new Test
        {
            TestCode = Code,
            ExpectedDiagnostics =
            {
                new DiagnosticResult("CA1034", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Keywords").WithIsSuppressed(true),
                new DiagnosticResult("CA1034", DiagnosticSeverity.Warning).WithLocation(1).WithArguments("Tasks").WithIsSuppressed(true),
                new DiagnosticResult("CA1034", DiagnosticSeverity.Warning).WithLocation(2).WithArguments("Opcodes").WithIsSuppressed(true),
                new DiagnosticResult("CA1034", DiagnosticSeverity.Warning).WithLocation(3).WithArguments("NotSuppressed").WithIsSuppressed(false)
            }
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EventSource派生クラスでなければ抑制されない()
    {
        /* lang=c#-test */
        const string Code =
            """
            public class MyEventSource
            {
                public static class {|#0:Keywords|}
                {
                }

                public static class {|#1:Tasks|}
                {
                }

                public static class {|#2:Opcodes|}
                {
                }
            }
            """;

        var test = new Test
        {
            TestCode = Code,
            ExpectedDiagnostics =
            {
                new DiagnosticResult("CA1034", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Keywords").WithIsSuppressed(false),
                new DiagnosticResult("CA1034", DiagnosticSeverity.Warning).WithLocation(1).WithArguments("Tasks").WithIsSuppressed(false),
                new DiagnosticResult("CA1034", DiagnosticSeverity.Warning).WithLocation(2).WithArguments("Opcodes").WithIsSuppressed(false)
            }
        };

        await test.RunAsync(this._testContext.CancellationToken).ConfigureAwait(false);
    }

    public EventSourceNestedTypeVisibilitySuppressorTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
