using System;
using System.Collections.Generic;

using Aetos.Tracing.Properties;

using Microsoft.CodeAnalysis;

namespace Aetos.Tracing;

internal static class DiagnosticDescriptors
{
    private static readonly DiagnosticDescriptor EventSourceClassMustHaveValidSignature = new(
        DiagnosticIds.EventSourceClassMustHaveValidSignature,
        CreateString(nameof(Resources.EventSourceClassMustHaveValidSignatureTitle)),
        CreateString(nameof(Resources.EventSourceClassMustHaveValidSignatureMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        CreateString(nameof(Resources.EventSourceClassMustHaveValidSignatureDescription)));

    private static readonly DiagnosticDescriptor EventSourceClassMustHaveValidEventSourceAttribute = new(
        DiagnosticIds.EventSourceClassMustHaveValidEventSourceAttribute,
        CreateString(nameof(Resources.EventSourceClassMustHaveValidEventSourceAttributeTitle)),
        CreateString(nameof(Resources.EventSourceClassMustHaveValidEventSourceAttributeMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        CreateString(nameof(Resources.EventSourceClassMustHaveValidEventSourceAttributeDescription)));

    private static readonly DiagnosticDescriptor EventSourceClassMustInheritFromEventSource = new(
        DiagnosticIds.EventSourceClassMustInheritFromEventSource,
        CreateString(nameof(Resources.EventSourceClassMustInheritFromEventSourceTitle)),
        CreateString(nameof(Resources.EventSourceClassMustInheritFromEventSourceMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        CreateString(nameof(Resources.EventSourceClassMustInheritFromEventSourceDescription)));

    private static readonly Dictionary<string, DiagnosticDescriptor> Descriptors = new(StringComparer.Ordinal)
    {
        [DiagnosticIds.EventSourceClassMustHaveValidSignature] = EventSourceClassMustHaveValidSignature,
        [DiagnosticIds.EventSourceClassMustHaveValidEventSourceAttribute] = EventSourceClassMustHaveValidEventSourceAttribute,
        [DiagnosticIds.EventSourceClassMustInheritFromEventSource] = EventSourceClassMustInheritFromEventSource
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
