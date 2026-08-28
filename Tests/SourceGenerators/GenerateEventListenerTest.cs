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
using Microsoft.CodeAnalysis.Testing;

using Shouldly;

using Aetos.EventSourceToolkit.Analyzers;
using Aetos.EventSourceToolkit.SourceGenerators;
using Aetos.EventSourceToolkit.Tests.TestUtilities;

namespace Aetos.EventSourceToolkit.Tests.SourceGenerators;

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

            using Aetos.EventSourceToolkit;

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

            using Aetos.EventSourceToolkit;

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
            (e, a, d) => diagnostics.Add(d),
            true,
            true,
            false,
            static (e) => true,
            null);

        var driverOptions = new GeneratorDriverOptions(
            trackIncrementalGeneratorSteps: true);

        var emitOptions = new EmitOptions(metadataOnly: true);

        var eventSourceSyntaxTree = CSharpSyntaxTree.ParseText(
            EventSourceCode,
            parseOptions,
            "EventSource.cs",
            Encoding.UTF8,
            testCancellationToken);

        var referenceAssemblies = await ReferenceAssemblies.Net.Net100
            .ResolveAsync(LanguageNames.CSharp, testCancellationToken)
            .ConfigureAwait(false);

        var eventSourceCompilation = CSharpCompilation.Create(
            "EventProducer",
            [eventSourceSyntaxTree],
            referenceAssemblies,
            compilationOptions);

        var eventSourceGeneratorDriver = (GeneratorDriver)CSharpGeneratorDriver.Create(
            [new EventSourceGenerator().AsSourceGenerator()],
            [],
            parseOptions,
            analyzerConfigOptionsProvider,
            driverOptions);

        eventSourceGeneratorDriver = eventSourceGeneratorDriver.RunGeneratorsAndUpdateCompilation(
            eventSourceCompilation,
            out var updatedEventSourceCompilation,
            out var eventSourceGeneratorDiagnostics,
            testCancellationToken);

        diagnostics.AddRange(updatedEventSourceCompilation.GetDiagnostics(testCancellationToken));
        diagnostics.AddRange(eventSourceGeneratorDiagnostics);

        var compilationWithAnalyzers = updatedEventSourceCompilation
            .WithAnalyzers(
                [new EventSourceClassSignatureAnalyzer(), new EventSourceNestedTypeVisibilitySuppressor()],
                analysisOptions);

        var analysisResult = await compilationWithAnalyzers
            .GetAnalysisResultAsync(testCancellationToken)
            .ConfigureAwait(false);

        diagnostics.AddRange(analysisResult.GetAllDiagnostics());

        using var eventSourcePeStream = new MemoryStream();

        var eventSourceEmitResult = compilationWithAnalyzers.Compilation.Emit(
            eventSourcePeStream,
            options: emitOptions,
            cancellationToken: testCancellationToken);

        diagnostics.AddRange(eventSourceEmitResult.Diagnostics);

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
            referenceAssemblies.Add(eventSourceMetadata.GetReference()),
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

        using var eventListenerPeStream = new MemoryStream();

        var eventListenerEmitResult = updatedEventListenerCompilation.Emit(
            eventListenerPeStream,
            options: emitOptions,
            cancellationToken: testCancellationToken);

        diagnostics.AddRange(eventListenerEmitResult.Diagnostics);

        foreach (var diagnostic in diagnostics)
        {
            testContext.WriteLine(diagnostic.ToString());
        }

        diagnostics.ShouldNotContain(static x => x.Severity == DiagnosticSeverity.Error);

        Assert.IsTrue(eventSourceEmitResult.Success);
        Assert.IsTrue(eventListenerEmitResult.Success);
    }

    public GenerateEventListenerTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
