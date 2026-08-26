using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using Basic.Reference.Assemblies;

using Shouldly;

namespace Aetos.Tracing.Tests;

[TestClass]
public sealed class GenerateEventListenerTest
{
    [TestMethod]
    public void X()
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

        var diagnostics = new List<Diagnostic>();

        var parseOptions = CSharpParseOptions.Default;

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: true,
            nullableContextOptions: NullableContextOptions.Enable);

        var driverOptions = new GeneratorDriverOptions(
            trackIncrementalGeneratorSteps: true);

        var emitOptions = new EmitOptions();

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

        var eventSourceGeneratorDriver = (GeneratorDriver)CSharpGeneratorDriver.Create(
            [new EventSourceAndListenerBaseGenerator().AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: driverOptions);

        eventSourceGeneratorDriver = eventSourceGeneratorDriver.RunGeneratorsAndUpdateCompilation(
            eventSourceCompilation,
            out var updatedEventSourceCompilation,
            out var eventSourceGeneratorDiagnostics,
            testCancellationToken);

        diagnostics.AddRange(updatedEventSourceCompilation.GetDiagnostics(testCancellationToken));
        diagnostics.AddRange(eventSourceGeneratorDiagnostics);

        using var eventSourcePeStream = new MemoryStream();
        using var eventSourcePdbStream = new MemoryStream();

        var emitResult = updatedEventSourceCompilation.Emit(
            eventSourcePeStream,
            eventSourcePdbStream,
            options: emitOptions,
            cancellationToken: testCancellationToken);

        foreach (var emitDiagnostic in emitResult.Diagnostics)
        {
            testContext.WriteLine(emitDiagnostic.ToString());
        }

        Assert.IsTrue(emitResult.Success);

        eventSourcePeStream.Position = 0;
        eventSourcePdbStream.Position = 0;

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
            parseOptions: parseOptions,
            driverOptions: driverOptions);

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
