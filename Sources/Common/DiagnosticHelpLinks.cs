namespace Aetos.EventSourceToolkit;

internal static class DiagnosticHelpLinks
{
    private const string BaseUri = "https://github.com/aetos382/EventSourceToolkit/blob/main/docs/diagnostics/";

    public static string GetHelpLinkUri(
        string id)
    {
        return $"{BaseUri}{id}.md";
    }
}
