using Microsoft.CodeAnalysis;

namespace Aetos.Tracing;

internal sealed record DiagnosticInfo(
    string Id,
    NodeLocationInfo Location)
{
    public Diagnostic CreateDiagnostic()
    {
        var descriptor = DiagnosticDescriptors.GetDescriptor(this.Id);

        var diagnostic = Diagnostic.Create(
            descriptor,
            this.Location.CreateLocation());

        return diagnostic;
    }
}
