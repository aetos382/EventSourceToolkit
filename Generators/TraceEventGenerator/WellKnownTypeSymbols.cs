using System;

using Microsoft.CodeAnalysis;

namespace Aetos.Tracing;

internal sealed class WellKnownTypeSymbols
{
    private readonly Compilation _compilation;

    public WellKnownTypeSymbols(Compilation compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        this._compilation = compilation;
    }

    public INamedTypeSymbol Int32
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_Int32);
            return field;
        }
    }

    public INamedTypeSymbol String
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_String);
            return field;
        }
    }

    public INamedTypeSymbol EventSource
    {
        get
        {
            field ??= this.GetTypeSymbol("System.Diagnostics.Tracing.EventSource");
            return field;
        }
    }

    public INamedTypeSymbol EventSourceAttribute
    {
        get
        {
            field ??= this.GetTypeSymbol("System.Diagnostics.Tracing.EventSourceAttribute");
            return field;
        }
    }

    public INamedTypeSymbol EventAttribute
    {
        get
        {
            field ??= this.GetTypeSymbol("System.Diagnostics.Tracing.EventAttribute");
            return field;
        }
    }

    public INamedTypeSymbol NonEventAttribute
    {
        get
        {
            field ??= this.GetTypeSymbol("System.Diagnostics.Tracing.NonEventAttribute");
            return field;
        }
    }

    public INamedTypeSymbol EventLevel
    {
        get
        {
            field ??= this.GetTypeSymbol("System.Diagnostics.Tracing.EventLevel");
            return field;
        }
    }

    public INamedTypeSymbol GeneratedEventSourceAttribute
    {
        get
        {
            field ??= this.GetTypeSymbol("Aetos.Tracing.GeneratedEventSourceAttribute");
            return field;
        }
    }

    private INamedTypeSymbol GetTypeSymbol(string metadataName)
    {
        return this._compilation.GetTypeByMetadataName(metadataName)!;
    }

    private INamedTypeSymbol GetTypeSymbol(SpecialType specialType)
    {
        return this._compilation.GetSpecialType(specialType);
    }
}
