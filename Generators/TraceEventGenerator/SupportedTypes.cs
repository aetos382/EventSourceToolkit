using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Aetos.Tracing;

internal sealed class SupportedTypes
{
    private readonly WellKnownTypeSymbols _wellKnownTypes;
    private readonly ITypeSymbol[] _supportedTypes;

    public SupportedTypes(
        WellKnownTypeSymbols wellKnownTypes)
    {
        this._wellKnownTypes = wellKnownTypes;
        var list = new List<ITypeSymbol>
        {
            wellKnownTypes.Boolean,
            wellKnownTypes.Char,
            wellKnownTypes.Byte,
            wellKnownTypes.SByte,
            wellKnownTypes.Int16,
            wellKnownTypes.UInt16,
            wellKnownTypes.Int32,
            wellKnownTypes.UInt32,
            wellKnownTypes.Int64,
            wellKnownTypes.UInt64,
            wellKnownTypes.Single,
            wellKnownTypes.Double,
            wellKnownTypes.IntPtr,
            wellKnownTypes.DateTime,
            wellKnownTypes.Guid,
            wellKnownTypes.String,
            wellKnownTypes.ByteArray,
            wellKnownTypes.BytePointer
        };

        this._supportedTypes = list.ToArray();
    }

    public bool IsSupported(ITypeSymbol type)
    {
        if (type.IsDerivedFrom(this._wellKnownTypes.Enum))
        {
            return true;
        }

        var comparer = SymbolEqualityComparer.Default;

        foreach (var supportedType in this._supportedTypes)
        {
            if (comparer.Equals(type, supportedType))
            {
                return true;
            }
        }

        return false;
    }
}
