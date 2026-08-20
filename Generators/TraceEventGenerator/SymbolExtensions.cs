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
            INamedTypeSymbol attributeType)
        {
            var comparer = SymbolEqualityComparer.Default;

            foreach (var attribute in symbol.GetAttributes())
            {
                if (comparer.Equals(attribute.AttributeClass, attributeType))
                {
                    yield return attribute;
                }
            }
        }

        public AttributeData? GetAttribute(
            INamedTypeSymbol attributeType)
        {
            AttributeData? found = null;

            foreach (var data in symbol.GetAttributes(attributeType))
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
            INamedTypeSymbol attributeType)
        {
            return symbol.GetAttributes(attributeType).Any();
        }
    }

    extension(ITypeSymbol symbol)
    {
        public bool IsDerivedFrom(INamedTypeSymbol baseType)
        {
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
    }
}
