using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit;

[Embedded]
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
            var foundInDerived = false;

            foreach (var attribute in symbol.GetAttributes())
            {
                if (comparer.Equals(attribute.AttributeClass, attributeType))
                {
                    foundInDerived = true;
                    yield return attribute;
                }
            }

            if (!inherited)
            {
                yield break;
            }

            var attributeInherited = true;
            var attributeAllowMultiple = false;

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
                    if (name == nameof(AttributeUsageAttribute.Inherited))
                    {
                        attributeInherited = (bool)value.Value!;
                    }
                    else if (name == nameof(AttributeUsageAttribute.AllowMultiple))
                    {
                        attributeAllowMultiple = (bool)value.Value!;
                    }
                }
            }

            if (!attributeInherited)
            {
                yield break;
            }

            if (!attributeAllowMultiple && foundInDerived)
            {
                // AllowMultiple = false かつ基底クラスと派生クラスの両方に付いている場合、派生側のみ返す
                yield break;
            }

            // Inherited = true でもインターフェイスの属性までは辿らない（リフレクションと同じ挙動）
            ISymbol? baseSymbol = symbol switch
            {
                ITypeSymbol t => t.BaseType,
                IMethodSymbol m => m.OverriddenMethod,
                IPropertySymbol p => p.OverriddenProperty,
                IEventSymbol e => e.OverriddenEvent,
                _ => null
            };

            if (baseSymbol is null)
            {
                yield break;
            }

            foreach (var attribute in baseSymbol.GetAttributes(attributeType, true))
            {
                yield return attribute;
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
                    // TODO: Resource
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
