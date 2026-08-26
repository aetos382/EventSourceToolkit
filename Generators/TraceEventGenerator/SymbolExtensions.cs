using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Aetos.Tracing;

internal static class SymbolExtensions
{
    extension(ISymbol symbol)
    {
        public IEnumerable<AttributeData> GetAttributes(
            INamedTypeSymbol attributeType,
            bool inherited = false)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            ArgumentNullException.ThrowIfNull(attributeType);

            var comparer = SymbolEqualityComparer.Default;

            foreach (var attribute in symbol.GetAttributes())
            {
                if (comparer.Equals(attribute.AttributeClass, attributeType))
                {
                    yield return attribute;
                }
            }

            if (!inherited)
            {
                yield break;
            }

            // Roslyn は AttributeUsageAttribute.Inherited を見てくれないので、自分で継承階層を辿る
            foreach (var attributeOfAttribute in attributeType.GetAttributes())
            {
                var fullName = attributeOfAttribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (fullName != "global::System.AttributeUsageAttribute")
                {
                    continue;
                }

                foreach (var (name, value) in attributeOfAttribute.NamedArguments)
                {
                    if (name != nameof(AttributeUsageAttribute.Inherited))
                    {
                        continue;
                    }

                    if ((bool)value.Value!)
                    {
                        break;
                    }

                    yield break;
                }
            }

            if (symbol.IsOverride)
            {
                if (symbol is IMethodSymbol { OverriddenMethod: {} baseMethodSymbol })
                {
                    foreach (var attribute in baseMethodSymbol.GetAttributes(attributeType, inherited))
                    {
                        yield return attribute;
                    }
                }
                else if (symbol is ITypeSymbol { BaseType: { } baseTypeSymbol })
                {
                    foreach (var attribute in baseTypeSymbol.GetAttributes(attributeType, inherited))
                    {
                        yield return attribute;
                    }
                }
            }
        }

        public AttributeData? GetAttribute(
            INamedTypeSymbol attributeType,
            bool inherited = false)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            ArgumentNullException.ThrowIfNull(attributeType);

            AttributeData? found = null;

            foreach (var data in symbol.GetAttributes(attributeType, inherited))
            {
                if (found is not null)
                {
                    throw new ArgumentException();
                }

                found = data;
            }

            return found;
        }

        public bool HasAttribute(
            INamedTypeSymbol attributeType,
            bool inherited = false)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            ArgumentNullException.ThrowIfNull(attributeType);

            return symbol.GetAttributes(attributeType, inherited).Any();
        }
    }

    extension(ITypeSymbol symbol)
    {
        public IEnumerable<IMethodSymbol> GetMethods()
        {
            ArgumentNullException.ThrowIfNull(symbol);

            return symbol.GetMembers().OfType<IMethodSymbol>();
        }

        public bool IsDerivedFrom(ITypeSymbol baseType)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            ArgumentNullException.ThrowIfNull(baseType);

            var currentSymbol = symbol;
            var comparer = SymbolEqualityComparer.Default;

            while (currentSymbol is not null)
            {
                if (comparer.Equals(currentSymbol, baseType))
                {
                    return true;
                }

                currentSymbol = currentSymbol.BaseType;
            }

            return false;
        }

        public string ToFullyQualifiedString()
        {
            ArgumentNullException.ThrowIfNull(symbol);

            // SymbolDisplayFormat では SymbolDisplayMiscellaneousOptions.UseSpecialTypes フラグが含まれていなくても
            // IntPtr / UIntPtr は "nint" / "nuint" になってしまうので、自力で文字列化する。
            // https://github.com/dotnet/roslyn/issues/76895
            var name = symbol.IsNativeIntegerType
                ? symbol.SpecialType == SpecialType.System_IntPtr
                    ? "global::System.IntPtr"
                    : "global::System.UIntPtr"
                : symbol.ToDisplayString(CustomSymbolDisplayFormats.FullyQualifiedFormat);

            return name;
        }
    }
}
