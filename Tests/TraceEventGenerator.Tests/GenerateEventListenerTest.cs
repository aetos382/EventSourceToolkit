using System;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using Basic.Reference.Assemblies;

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
            out var eventSourceDiagnostics,
            testCancellationToken);

        foreach (var eventSourceDiagnostic in eventSourceDiagnostics)
        {
            testContext.WriteLine(eventSourceDiagnostic.ToString());
        }

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
            """
            using Aetos.Tracing;

            namespace Samples;

            [GeneratedEventListener("SampleEvents")]
            internal sealed partial class SampleEventListener : SampleEventSource.ListenerBase
            {
                public override void Foo()
                {
                }
            }
            """);

        var eventSourceAssemblyReference = MetadataReference.CreateFromStream(eventSourcePeStream);

        var eventListenerCompilation = CSharpCompilation.Create(
            "EventConsumer",
            [eventListenerSyntaxTree],
            [
                .. Net100.References.All,
                eventSourceAssemblyReference
            ],
            compilationOptions);

        var eventListenerGeneratorDriver = (GeneratorDriver)CSharpGeneratorDriver.Create(
            [new EventListenerGenerator().AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: driverOptions);

        eventListenerGeneratorDriver = eventListenerGeneratorDriver.RunGenerators(
            eventListenerCompilation, testCancellationToken);

        var eventListenerGenerationResult = eventListenerGeneratorDriver.GetRunResult();

        foreach (var eventListenerDiagnostic in eventListenerGenerationResult.Diagnostics)
        {
            testContext.WriteLine(eventListenerDiagnostic.ToString());
        }
    }

    public GenerateEventListenerTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
