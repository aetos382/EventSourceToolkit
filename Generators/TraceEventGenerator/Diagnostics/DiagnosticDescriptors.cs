using System;
using System.Collections.Generic;

using Aetos.Tracing.Properties;

using Microsoft.CodeAnalysis;

namespace Aetos.Tracing.Diagnostics;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor EventSourceClassMustHaveValidSignature = new(
        DiagnosticIds.EventSourceClassMustHaveValidSignature,
        CreateString(nameof(Resources.EventSourceClassMustHaveValidSignatureTitle)),
        CreateString(nameof(Resources.EventSourceClassMustHaveValidSignatureMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        CreateString(nameof(Resources.EventSourceClassMustHaveValidSignatureDescription)));

    public static readonly DiagnosticDescriptor EventSourceClassMustHaveValidEventSourceAttribute = new(
        DiagnosticIds.EventSourceClassMustHaveValidEventSourceAttribute,
        CreateString(nameof(Resources.EventSourceClassMustHaveValidEventSourceAttributeTitle)),
        CreateString(nameof(Resources.EventSourceClassMustHaveValidEventSourceAttributeMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        CreateString(nameof(Resources.EventSourceClassMustHaveValidEventSourceAttributeDescription)));

    public static readonly DiagnosticDescriptor EventSourceClassMustInheritFromEventSource = new(
        DiagnosticIds.EventSourceClassMustInheritFromEventSource,
        CreateString(nameof(Resources.EventSourceClassMustInheritFromEventSourceTitle)),
        CreateString(nameof(Resources.EventSourceClassMustInheritFromEventSourceMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        CreateString(nameof(Resources.EventSourceClassMustInheritFromEventSourceDescription)));

    public static readonly DiagnosticDescriptor EventSourceMethodMustHaveValidSignature = new(
        DiagnosticIds.EventSourceMethodMustHaveValidSignature,
        CreateString(nameof(Resources.EventSourceMethodMustHaveValidSignatureTitle)),
        CreateString(nameof(Resources.EventSourceMethodMustHaveValidSignatureMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        CreateString(nameof(Resources.EventSourceMethodMustHaveValidSignatureDescription)));

    public static readonly DiagnosticDescriptor EventSourceMethodMustHaveValidAttributes = new(
        DiagnosticIds.EventSourceMethodMustHaveValidAttributes,
        CreateString(nameof(Resources.EventSourceMethodMustHaveValidAttributesTitle)),
        CreateString(nameof(Resources.EventSourceMethodMustHaveValidAttributesMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Warning,
        true,
        CreateString(nameof(Resources.EventSourceMethodMustHaveValidAttributesDescription)));

    public static readonly DiagnosticDescriptor EventSourceMethodShouldHaveEventAttribute = new(
        DiagnosticIds.EventSourceMethodShouldHaveEventAttribute,
        CreateString(nameof(Resources.EventSourceMethodShouldHaveEventAttributeTitle)),
        CreateString(nameof(Resources.EventSourceMethodShouldHaveEventAttributeMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Warning,
        true,
        CreateString(nameof(Resources.EventSourceMethodShouldHaveEventAttributeDescription)));

    private static readonly Dictionary<string, DiagnosticDescriptor> Descriptors = new(StringComparer.Ordinal)
    {
        [DiagnosticIds.EventSourceClassMustHaveValidSignature] = EventSourceClassMustHaveValidSignature,
        [DiagnosticIds.EventSourceClassMustHaveValidEventSourceAttribute] = EventSourceClassMustHaveValidEventSourceAttribute,
        [DiagnosticIds.EventSourceClassMustInheritFromEventSource] = EventSourceClassMustInheritFromEventSource,
        [DiagnosticIds.EventSourceMethodMustHaveValidSignature] = EventSourceMethodMustHaveValidSignature,
        [DiagnosticIds.EventSourceMethodMustHaveValidAttributes] = EventSourceMethodMustHaveValidAttributes,
        [DiagnosticIds.EventSourceMethodShouldHaveEventAttribute] = EventSourceMethodShouldHaveEventAttribute,
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
