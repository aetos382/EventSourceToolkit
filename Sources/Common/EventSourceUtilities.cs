using System;

using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit;

public static class EventSourceUtilities
{
    public static bool IsSupportedParameterType(
        ITypeSymbol typeSymbol,
        WellKnownSymbols wellKnownSymbols)
    {
        ArgumentNullException.ThrowIfNull(typeSymbol);
        ArgumentNullException.ThrowIfNull(wellKnownSymbols);

        var typeKind = typeSymbol.TypeKind;
        var specialType = typeSymbol.SpecialType;

        if (specialType is not SpecialType.None)
        {
            if (specialType is (
                SpecialType.System_Boolean or
                SpecialType.System_Byte or
                SpecialType.System_Char or
                SpecialType.System_DateTime or
                SpecialType.System_Decimal or
                SpecialType.System_Double or
                SpecialType.System_Int16 or
                SpecialType.System_Int32 or
                SpecialType.System_Int64 or
                SpecialType.System_IntPtr or
                SpecialType.System_SByte or
                SpecialType.System_Single or
                SpecialType.System_String or
                SpecialType.System_UInt16 or
                SpecialType.System_UInt32 or
                SpecialType.System_UInt64))
            {
                return true;
            }
        }

        if (typeKind is TypeKind.Enum)
        {
            if (typeSymbol is INamedTypeSymbol { EnumUnderlyingType: { } enumUnderlyingType })
            {
                if (enumUnderlyingType.SpecialType is (
                    SpecialType.System_Byte or
                    SpecialType.System_Int16 or
                    SpecialType.System_Int32 or
                    SpecialType.System_Int64 or
                    SpecialType.System_SByte or
                    SpecialType.System_UInt16 or
                    SpecialType.System_UInt32 or
                    SpecialType.System_UInt64))
                {
                    return true;
                }
            }
        }

        if (typeKind is TypeKind.Array)
        {
            if (typeSymbol is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
            {
                return true;
            }
        }

        var comparer = SymbolEqualityComparer.Default;
        if (comparer.Equals(typeSymbol, wellKnownSymbols.Guid))
        {
            return true;
        }

        return false;
    }
}
