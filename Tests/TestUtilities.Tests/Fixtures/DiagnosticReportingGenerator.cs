using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities.Tests.Fixtures;

/// <summary>位置を持たない診断を 1 件報告するだけの、テスト用のジェネレーター。</summary>
internal sealed class DiagnosticReportingGenerator :
    IIncrementalGenerator
{
    public const string DiagnosticId = "TESTGEN01";

    private static readonly DiagnosticDescriptor Descriptor = new(
        DiagnosticId,
        "ジェネレーターからの報告",
        "ジェネレーターからの報告",
        "Test",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public void Initialize(
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(
            context.CompilationProvider,
            static (context, _) => context.ReportDiagnostic(
                Diagnostic.Create(Descriptor, Location.None)));
    }
}
