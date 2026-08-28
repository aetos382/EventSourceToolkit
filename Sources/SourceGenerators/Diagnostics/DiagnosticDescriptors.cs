using System;
using System.Collections.Generic;

using Aetos.EventSourceToolkit.SourceGenerators.Properties;

using Microsoft.CodeAnalysis;

namespace Aetos.EventSourceToolkit.SourceGenerators.Diagnostics;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor ParameterTypeNotSupported = new(
        DiagnosticIds.ParameterTypeNotSupported,
        CreateString(nameof(Resources.ParameterTypeNotSupportedTitle)),
        CreateString(nameof(Resources.ParameterTypeNotSupportedMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        CreateString(nameof(Resources.ParameterTypeNotSupportedDescription)));

    private static readonly Dictionary<string, DiagnosticDescriptor> Descriptors = new(StringComparer.Ordinal)
    {
        [DiagnosticIds.ParameterTypeNotSupported] = ParameterTypeNotSupported
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
