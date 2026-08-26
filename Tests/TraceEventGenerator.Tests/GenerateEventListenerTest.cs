using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;

using Basic.Reference.Assemblies;

using Shouldly;

namespace Aetos.Tracing.Tests;

[TestClass]
public sealed class GenerateEventListenerTest
{
    [TestMethod]
    public async Task X()
    {
        var testContext = this._testContext;
        var testCancellationToken = testContext.CancellationToken;

        const string EventSourceCode =
            """
            using System;
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            namespace Samples;

            [EventSource(Name = "SampleEvents")]
            [GeneratedEventSource]
            public sealed partial class SampleEventSource : EventSource
            {
                [Event(1)]
                public partial void Foo();

                [Event(2)]
                public partial void Bar(int i);

                [Event(3)]
                public partial void Baz(Guid relatedActivityId);

                [Event(4)]
                public partial void Qux(Guid relatedActivityId, int i);
            }
            """;

        const string EventListenerCode =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            namespace Samples;

            [GeneratedEventListener("SampleEvents")]
            internal sealed partial class SampleEventListener : SampleEventSource.ListenerBase
            {
                protected override void Foo(EventWrittenEventArgs args)
                {
                }
            }
            """;

        var analyzerConfigOptionsProvider = new TestAnalyzerConfigOptionsProvider();

        var diagnostics = new List<Diagnostic>();

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

        var driverOptions = new GeneratorDriverOptions(
            trackIncrementalGeneratorSteps: true);

        var emitOptions = new EmitOptions(metadataOnly: true);

        var eventSourceSyntaxTree = CSharpSyntaxTree.ParseText(
            EventSourceCode,
            parseOptions,
            "EventSource.cs",
            Encoding.UTF8,
            testCancellationToken);

        var eventSourceCompilation = CSharpCompilation.Create(
            "EventProducer",
            [eventSourceSyntaxTree],
            Net100.References.All,
            compilationOptions);

        var compilationWithAnalyzers = eventSourceCompilation
            .WithAnalyzers(
                [new EventSourceAnalyzer()],
                analysisOptions);

        var analysisResult = await compilationWithAnalyzers
            .GetAnalysisResultAsync(testCancellationToken)
            .ConfigureAwait(false);

        diagnostics.AddRange(analysisResult.GetAllDiagnostics());

        var eventSourceGeneratorDriver = (GeneratorDriver)CSharpGeneratorDriver.Create(
            [new EventSourceAndListenerBaseGenerator().AsSourceGenerator()],
            [],
            parseOptions,
            analyzerConfigOptionsProvider,
            driverOptions);

        eventSourceGeneratorDriver = eventSourceGeneratorDriver.RunGeneratorsAndUpdateCompilation(
            compilationWithAnalyzers.Compilation,
            out var updatedEventSourceCompilation,
            out var eventSourceGeneratorDiagnostics,
            testCancellationToken);

        diagnostics.AddRange(updatedEventSourceCompilation.GetDiagnostics(testCancellationToken));
        diagnostics.AddRange(eventSourceGeneratorDiagnostics);

        using var eventSourcePeStream = new MemoryStream();

        var emitResult = updatedEventSourceCompilation.Emit(
            eventSourcePeStream,
            options: emitOptions,
            cancellationToken: testCancellationToken);

        foreach (var emitDiagnostic in emitResult.Diagnostics)
        {
            testContext.WriteLine(emitDiagnostic.ToString());
        }

        Assert.IsTrue(emitResult.Success);

        eventSourcePeStream.Position = 0;

        var eventListenerSyntaxTree = CSharpSyntaxTree.ParseText(
            EventListenerCode,
            parseOptions,
            "EventListener.cs",
            Encoding.UTF8,
            testCancellationToken);

        using var eventSourceMetadata = AssemblyMetadata.CreateFromStream(eventSourcePeStream, PEStreamOptions.Default);

        var eventListenerCompilation = CSharpCompilation.Create(
            "EventConsumer",
            [eventListenerSyntaxTree],
            [
                .. Net100.References.All,
                eventSourceMetadata.GetReference()
            ],
            compilationOptions);

        var eventListenerGeneratorDriver = (GeneratorDriver)CSharpGeneratorDriver.Create(
            [new EventListenerGenerator().AsSourceGenerator()],
            [],
            parseOptions,
            analyzerConfigOptionsProvider,
            driverOptions);

        eventListenerGeneratorDriver = eventListenerGeneratorDriver.RunGeneratorsAndUpdateCompilation(
            eventListenerCompilation,
            out var updatedEventListenerCompilation,
            out var eventListenerGeneratorDiagnostics,
            testCancellationToken);

        diagnostics.AddRange(updatedEventListenerCompilation.GetDiagnostics(testCancellationToken));
        diagnostics.AddRange(eventListenerGeneratorDiagnostics);

        foreach (var diagnostic in diagnostics)
        {
            testContext.WriteLine(diagnostic.ToString());
        }

        diagnostics.ShouldNotContain(static x => x.Severity == DiagnosticSeverity.Error);
    }

    public GenerateEventListenerTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
