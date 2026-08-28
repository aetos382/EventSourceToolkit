using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

public class CSharpAnalyzerTest : AnalyzerTest<DefaultVerifier>
{
    /// <inheritdoc />
    protected override CompilationOptions CreateCompilationOptions()
    {
        return new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: true,
            nullableContextOptions: NullableContextOptions.Enable);
    }

    /// <inheritdoc />
    protected override ParseOptions CreateParseOptions()
    {
        return CSharpParseOptions.Default;
    }

    /// <inheritdoc />
    protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => [];

    /// <inheritdoc />
    protected override string DefaultFileExt => ".cs";

    /// <inheritdoc />
    public override string Language => LanguageNames.CSharp;
}
