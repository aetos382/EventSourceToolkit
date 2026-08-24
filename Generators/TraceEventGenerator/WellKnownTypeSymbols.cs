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

    public INamedTypeSymbol Boolean
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_Boolean);
            return field;
        }
    }

    public INamedTypeSymbol Char
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_Char);
            return field;
        }
    }

    public INamedTypeSymbol SByte
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_SByte);
            return field;
        }
    }

    public INamedTypeSymbol Byte
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_Byte);
            return field;
        }
    }

    public INamedTypeSymbol Int16
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_Int16);
            return field;
        }
    }

    public INamedTypeSymbol UInt16
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_UInt16);
            return field;
        }
    }

    public INamedTypeSymbol Int32
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_Int32);
            return field;
        }
    }

    public INamedTypeSymbol UInt32
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_UInt32);
            return field;
        }
    }

    public INamedTypeSymbol Int64
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_Int64);
            return field;
        }
    }

    public INamedTypeSymbol UInt64
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_UInt64);
            return field;
        }
    }

    public INamedTypeSymbol Single
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_Single);
            return field;
        }
    }

    public INamedTypeSymbol Double
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_Double);
            return field;
        }
    }

    public INamedTypeSymbol IntPtr
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_IntPtr);
            return field;
        }
    }

    public INamedTypeSymbol DateTime
    {
        get
        {
            field ??= this.GetTypeSymbol(SpecialType.System_DateTime);
            return field;
        }
    }

    public INamedTypeSymbol Guid
    {
        get
        {
            field ??= this.GetTypeSymbol("System.Guid");
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

    public IArrayTypeSymbol ByteArray
    {
        get
        {
            field ??= this._compilation.CreateArrayTypeSymbol(this.Byte);
            return field;
        }
    }

    public IPointerTypeSymbol BytePointer
    {
        get
        {
            field ??= this._compilation.CreatePointerTypeSymbol(this.Byte);
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
