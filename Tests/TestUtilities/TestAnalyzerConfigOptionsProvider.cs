using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

public sealed class TestAnalyzerConfigOptionsProvider :
    AnalyzerConfigOptionsProvider
{
    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
    {
        return TestAnalyzerConfigOptions.Empty;
    }

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
    {
        return TestAnalyzerConfigOptions.Empty;
    }

    /// <inheritdoc />
    public override AnalyzerConfigOptions GlobalOptions => TestAnalyzerConfigOptions.Empty;

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        /// <inheritdoc />
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            value = null;
            return false;
        }

        public static readonly TestAnalyzerConfigOptions Empty = new();
    }
}
