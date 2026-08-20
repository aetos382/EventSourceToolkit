using System;
using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Basic.Reference.Assemblies;

namespace Aetos.Tracing.Tests;

[TestClass]
public sealed class EventSourceGeneratorTest
{
    [TestMethod]
    public void とりあえず正常系()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class InvalidEventSource : EventSource
            {
                [Event(1)]
                public partial void Foo();
            }
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var result = RunGenerator(Code, testCancellationToken);

        Assert.IsEmpty(result.GeneratorDiagnostics);
    }

    [TestMethod]
    public void partial修飾子がないクラスにはTEG001が出る()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            class InvalidEventSource : EventSource;
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var result = RunGenerator(Code, testCancellationToken);

        Assert.Contains(
            static diagnostic => diagnostic.Id == DiagnosticIds.EventSourceClassMustHaveValidSignature,
            result.GeneratorDiagnostics);
    }

    [TestMethod]
    public void file修飾子があるクラスにはTEG001が出る()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            file partial class InvalidEventSource : EventSource;
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var result = RunGenerator(Code, testCancellationToken);

        Assert.Contains(
            static diagnostic => diagnostic.Id == DiagnosticIds.EventSourceClassMustHaveValidSignature,
            result.GeneratorDiagnostics);
    }

    [TestMethod]
    public void EventSourceAttributeがないクラスにはTEG002が出る()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            // [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class InvalidEventSource : EventSource;
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var result = RunGenerator(Code, testCancellationToken);

        Assert.Contains(
            static diagnostic => diagnostic.Id == DiagnosticIds.EventSourceClassMustHaveValidEventSourceAttribute,
            result.GeneratorDiagnostics);
    }

    [TestMethod]
    public void EventSourceAttributeのNameがnullなクラスにはTEG002が出る()
    {
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource]
            [GeneratedEventSource]
            partial class InvalidEventSource;
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var result = RunGenerator(Code, testCancellationToken);

        Assert.Contains(
            static diagnostic => diagnostic.Id == DiagnosticIds.EventSourceClassMustHaveValidEventSourceAttribute,
            result.GeneratorDiagnostics);
    }

    [TestMethod]
    public void EventSourceから派生していないクラスにはTEG003が出る()
    {
        // lang=csharp
        const string Code =
            """
            using System.Diagnostics.Tracing;

            using Aetos.Tracing;

            [EventSource(Name = "TestEventSource")]
            [GeneratedEventSource]
            partial class InvalidEventSource;
            """;

        var testCancellationToken = this._testContext.CancellationToken;

        var result = RunGenerator(Code, testCancellationToken);

        Assert.Contains(
            static diagnostic => diagnostic.Id == DiagnosticIds.EventSourceClassMustInheritFromEventSource,
            result.GeneratorDiagnostics);
    }

    private sealed record GenerateResult(
        CSharpGeneratorDriver Driver,
        CSharpCompilation OriginalCompilation,
        CSharpCompilation UpdatedCompilation,
        ImmutableArray<Diagnostic> GeneratorDiagnostics);

    private static GenerateResult RunGenerator(
        string code,
        CancellationToken cancellationToken)
    {
        var parseOptions = CSharpParseOptions.Default;

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary);

        var driverOptions = new GeneratorDriverOptions(
            IncrementalGeneratorOutputKind.None, true);

        var syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions, cancellationToken: cancellationToken);

        var compilation = CSharpCompilation.Create(
            null,
            [syntaxTree],
            Net100.References.All,
            compilationOptions);

        var generator = new TraceEventGenerator();

        var driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: driverOptions);

        var driver2 = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out var generatorDiagnostics,
            cancellationToken);

        return new(
            (CSharpGeneratorDriver)driver2,
            compilation,
            (CSharpCompilation)updatedCompilation,
            generatorDiagnostics);
    }

    public EventSourceGeneratorTest(
        TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        this._testContext = testContext;
    }

    private readonly TestContext _testContext;
}
