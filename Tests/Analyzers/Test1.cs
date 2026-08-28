using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Aetos.EventSourceToolkit.Analyzers;
using Aetos.EventSourceToolkit.SourceGenerators;

namespace Aetos.EventSourceToolkit.Tests.Analyzers;

[TestClass]
public sealed class Test1
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
        /* lang=c#-test */
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
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
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
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
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
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
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
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
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
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
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
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task イベントソースクラスのpartialパーツのいずれかがEventSourceから派生していればEST003は出ない()
    {
        /* lang=c#-test */
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
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
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
            TestCode = Code,
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        await test.RunAsync(testCancellationToken).ConfigureAwait(false);
    }

    /*
    private static Task<ImmutableArray<Diagnostic>> RunBuildAsync(
        string code,
        ImmutableArray<IIncrementalGenerator> sourceGenerators,
        ImmutableArray<DiagnosticAnalyzer> diagnosticAnalyzers,
        AnalyzerConfigOptionsProvider optionsProvider,
        CancellationToken cancellationToken)
    {
        var generators = ImmutableArray.CreateBuilder<ISourceGenerator>(sourceGenerators.Length);
        foreach (var generator in sourceGenerators)
        {
            generators.Add(generator.AsSourceGenerator());
        }

        return RunBuildAsync(
            code,
            generators.DrainToImmutable(),
            diagnosticAnalyzers,
            optionsProvider,
            cancellationToken);
    }

    private static async Task<ImmutableArray<Diagnostic>> RunBuildAsync(
        string code,
        ImmutableArray<ISourceGenerator> sourceGenerators,
        ImmutableArray<DiagnosticAnalyzer> diagnosticAnalyzers,
        AnalyzerConfigOptionsProvider optionsProvider,
        CancellationToken cancellationToken)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var parseOptions = CSharpParseOptions.Default;

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: true,
            nullableContextOptions: NullableContextOptions.Enable);

        var generatorDriverOptions = new GeneratorDriverOptions(
            trackIncrementalGeneratorSteps: true);

        var analysisOptions = new CompilationWithAnalyzersOptions(
            new(
                [],
                optionsProvider),
            (e, a, d) => diagnostics.Add(d),
            true,
            true,
            false,
            static (e) => true,
            (a) => optionsProvider);

        var syntaxTree = CSharpSyntaxTree.ParseText(
            code,
            parseOptions,
            "main.cs",
            Encoding.UTF8,
            cancellationToken);

        var compilation = (Compilation)CSharpCompilation.Create(
            "test",
            [syntaxTree],
            Net100.References.All,
            compilationOptions);

        if (sourceGenerators.Length == 0)
        {
            diagnostics.AddRange(compilation.GetDiagnostics(cancellationToken));
        }
        else
        {
            var generatorDriver = (GeneratorDriver)CSharpGeneratorDriver.Create(
                sourceGenerators,
                [],
                parseOptions,
                optionsProvider,
                generatorDriverOptions);

            generatorDriver = generatorDriver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var updatedCompilation,
                out var generatorDiagnostics,
                cancellationToken);

            compilation = updatedCompilation;
            diagnostics.AddRange(generatorDiagnostics);
        }

        if (diagnosticAnalyzers.Length != 0)
        {
            var compilationWithAnalyzers = compilation.WithAnalyzers(
                diagnosticAnalyzers,
                analysisOptions);

            var analysisResult = await compilationWithAnalyzers
                .GetAnalysisResultAsync(cancellationToken)
                .ConfigureAwait(false);

            diagnostics.AddRange(analysisResult.GetAllDiagnostics());
        }

        return diagnostics.ToImmutable();
    }
    */

    public Test1(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
