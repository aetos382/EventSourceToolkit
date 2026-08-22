using System;
using System.Collections.Generic;

using Aetos.Tracing.Properties;

using Microsoft.CodeAnalysis;

namespace Aetos.Tracing.Diagnostics;

internal static class DiagnosticDescriptors
{
    private static readonly Dictionary<string, DiagnosticDescriptor> Descriptors = new(StringComparer.Ordinal)
    {
    };

    public static DiagnosticDescriptor GetDescriptor(string id)
    {
        return Descriptors[id];
    }

    private static LocalizableResourceString CreateString(string name)
    {
        return new(name, Resources.ResourceManager, typeof(Resources));
    }
}
