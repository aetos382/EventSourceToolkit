namespace Aetos.EventSourceToolkit;

public static class DiagnosticHelpLinks
{
    private const string BaseUri = "https://github.com/aetos382/EventSourceToolkit/blob/main/docs/diagnostics/";

#pragma warning disable CA1055
    public static string GetHelpLinkUri(
        string id)
    {
        return $"{BaseUri}{id}.md";
    }
#pragma warning restore
}
