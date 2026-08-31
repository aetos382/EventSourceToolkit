# Diagnostics

EventSourceToolkit reports the following diagnostics. Every diagnostic ID is unique across the whole
toolkit, regardless of whether it is reported by the analyzer or by the source generator.

| Rule ID | Title | Category | Severity | Reported by |
|---------|-------|----------|----------|-------------|
| [EST001](EST001.md) | An event source class must be a partial class | General | Error | Analyzer |
| [EST002](EST002.md) | An event source class must not be an abstract class | General | Error | Analyzer |
| [EST003](EST003.md) | An event source class must derive from System.Diagnostics.Tracing.EventSource | General | Error | Analyzer |
| [EST004](EST004.md) | An event source class must not be a file-local class | General | Error | Analyzer |
| [EST005](EST005.md) | An event method parameter must have a supported type | General | Error | Analyzer |

## How to read these documents

Each document is organised the same way:

- **Cause** — the condition that makes the diagnostic appear.
- **Rule description** — why the rule exists. Read this before deciding to work around it.
- **How to fix violations** — the change to make in your code.
- **When to suppress** — whether suppressing is ever reasonable, and what happens if you do.
- **Example** — a minimal violation and its fix.

The title of each document is the same normative sentence used as the diagnostic title in the IDE, and
the `Cause` section restates the message you see in the error list.
