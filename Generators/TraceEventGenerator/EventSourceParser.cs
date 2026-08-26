using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Aetos.Tracing.Diagnostics;
using Aetos.Tracing.Models;

namespace Aetos.Tracing;

internal sealed class EventSourceParser
{
    private readonly SemanticModel _semanticModel;
    private readonly WellKnownTypeSymbols _wellKnownTypes;

    public EventSourceParser(
        SemanticModel semanticModel)
    {
        ArgumentNullException.ThrowIfNull(semanticModel);

        this._semanticModel = semanticModel;
        this._wellKnownTypes = new WellKnownTypeSymbols(semanticModel.Compilation);
    }

    public EventSourceMethodInfoWithDiagnostics ParseEventSource(
        MethodDeclarationSyntax syntaxNode,
        IMethodSymbol symbol,
        CancellationToken cancellationToken)
    {
        var containingType = symbol.ContainingType;
        var wellKnownTypes = this._wellKnownTypes;
        var diagnostics = new List<DiagnosticInfo>();

        // クラスに対する警告はメソッド単位でのコード生成では大変なので、別の Analyzer を用意する

        // メソッドを含むクラスに GeneratedEventSourceAttribute がついていない → 無視, 生成対象外
        var markerAttribute = containingType.GetAttribute(wellKnownTypes.GeneratedEventSourceAttribute);
        if (markerAttribute is null)
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        // メソッドを含むクラスが（間接的に）EventSource から派生していない → 警告, 生成対象外
        if (!this.IsDerivedFromEventSource(containingType))
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        // メソッドを含むクラスに EventSourceAttribute がついていない → 警告, 生成対象外
        if (this.GetEventSourceAttribute(containingType) is null)
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        // メソッドが partial でない → 無視, 生成対象外
        if (!syntaxNode.HasPartialModifier)
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        // メソッドの実装が存在する → 無視, 生成対象外
        if (symbol.PartialImplementationPart is not null)
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        // メソッドの戻り値が void でない → 無視, 生成対象外
        if (!syntaxNode.ReturnsVoid)
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        var keywordsType = containingType.GetTypeMembers("Keywords").SingleOrDefault();

        var eventMetadata = this.ParseEventAttribute(
            symbol.GetAttribute(wellKnownTypes.EventAttribute)!,
            keywordsType);

        var parameterList = syntaxNode.ParameterList.Parameters;
        var parameters = new List<EventSourceMethodParameterInfo>(parameterList.Count);

        var semanticModel = this._semanticModel;
        var supportedTypes = new SupportedTypes(wellKnownTypes);
        var comparer = SymbolEqualityComparer.Default;

        foreach (var (index, parameter) in parameterList.Index())
        {
            var parameterName = parameter.Identifier.ValueText;
            var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken)!;
            var parameterTypeSymbol = parameterSymbol.Type;
            var isRelatedActivityIdParameter = false;

            if (index == 0 &&
                string.Equals(parameterName, "relatedActivityId", StringComparison.OrdinalIgnoreCase) &&
                comparer.Equals(parameterTypeSymbol, wellKnownTypes.Guid))
            {
                isRelatedActivityIdParameter = true;
            }

            if (!supportedTypes.IsSupported(parameterTypeSymbol))
            {
                // そのメソッドのパラメーターはサポートされているか
                diagnostics.Add(new DiagnosticInfo(DiagnosticIds.ParameterTypeNotSupported, parameter.GetNodeLocationInfo()));
            }

            var parameterTypeName = parameterTypeSymbol.ToFullyQualifiedString();

            var isEnum = false;
            var size = this.GetParameterSize(parameterTypeName);
            if (size is null)
            {
                if (parameterTypeSymbol is INamedTypeSymbol { EnumUnderlyingType: { } enumUnderlyingType })
                {
                    isEnum = true;
                    size = this.GetParameterSize(enumUnderlyingType.ToFullyQualifiedString());
                }
            }

            var parameterInfo = new EventSourceMethodParameterInfo(parameterTypeName, parameter.Identifier.ValueText, isEnum, size, isRelatedActivityIdParameter);
            parameters.Add(parameterInfo);
        }

        var ancestorTypes = new List<ContainingTypeInfo>();
        var namespaceSegments = new List<string>();

        var parentNode = syntaxNode.Parent;
        while (parentNode is not null and not CompilationUnitSyntax)
        {
            if (parentNode is TypeDeclarationSyntax parentTypeNode)
            {
                ancestorTypes.Insert(0, new(parentTypeNode.Keyword.Text, parentTypeNode.Identifier.Text));
            }
            else if (parentNode is BaseNamespaceDeclarationSyntax { Name: var name })
            {
                while (true)
                {
                    if (name is IdentifierNameSyntax identifierName)
                    {
                        namespaceSegments.Insert(0, identifierName.Identifier.Text);
                        break;
                    }

                    if (name is QualifiedNameSyntax qualifiedName)
                    {
                        namespaceSegments.Insert(0, qualifiedName.Right.Identifier.Text);
                        name = qualifiedName.Left;
                    }
                }

                break;
            }

            parentNode = parentNode.Parent;
        }

        var methodInfo = new EventSourceMethodInfo(
            namespaceSegments.ToArray(),
            ancestorTypes.ToArray(),
            syntaxNode.AccessibilityKeyword,
            syntaxNode.Identifier.Text,
            eventMetadata,
            parameters.ToArray());

        var methodInfoWithDiagnostics = new EventSourceMethodInfoWithDiagnostics(
            methodInfo,
            diagnostics.ToArray());

        return methodInfoWithDiagnostics;
    }

    private static bool IsValidClassModifiers(
        ClassDeclarationSyntax node)
    {
        return node is { HasPartialModifier: true, HasFileModifier: false };
    }

    private AttributeData? GetEventSourceAttribute(INamedTypeSymbol type)
    {
        return type.GetAttribute(this._wellKnownTypes.EventSourceAttribute);
    }

    private string? GetEventSourceName(INamedTypeSymbol type)
    {
        var wellKnownTypes = this._wellKnownTypes;

        var attribute = type.GetAttribute(wellKnownTypes.EventSourceAttribute);
        if (attribute is null)
        {
            return null;
        }

        var comparer = SymbolEqualityComparer.Default;

        foreach (var (name, value) in attribute.NamedArguments)
        {
            if (name != nameof(EventSourceAttribute.Name))
            {
                continue;
            }

            if (!comparer.Equals(value.Type, wellKnownTypes.String))
            {
                continue;
            }

            return (string?)value.Value;
        }

        return null;
    }

    private bool IsDerivedFromEventSource(INamedTypeSymbol type)
    {
        return type.IsDerivedFrom(this._wellKnownTypes.EventSource);
    }

    private EventMetadataInfo ParseEventAttribute(
        AttributeData data,
        INamedTypeSymbol? keywordsTypeSymbol)
    {
        var wellKnownTypes = this._wellKnownTypes;
        var comparer = SymbolEqualityComparer.Default;

        var id = 0;
        var level = nameof(EventLevel.Informational);
        var keywords = new List<string>();

        (EventKeywords Keyword, IFieldSymbol Symbol)[] eventKeywords = [];

        if (keywordsTypeSymbol is not null)
        {
            eventKeywords = keywordsTypeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(static x => x is { IsConst: true, HasConstantValue: true })
                .Where(x => comparer.Equals(x.Type, wellKnownTypes.EventKeywords))
                .Select(static x => (Keyword: (EventKeywords)x.ConstantValue!, Symbol: x))
                .ToArray();
        }

        var ctorArgs = data.ConstructorArguments;
        if (ctorArgs.Length == 1)
        {
            var idArg = ctorArgs[0];
            if (idArg.Kind == TypedConstantKind.Primitive && comparer.Equals(idArg.Type, wellKnownTypes.Int32) && idArg.Value is int idValue)
            {
                id = idValue;
            }
        }

        var namedArgs = data.NamedArguments;
        foreach (var (key, value) in namedArgs)
        {
            switch (key)
            {
                case nameof(EventAttribute.Level):
                    level = $"global::System.Diagnostics.Tracing.EventLevel.{Enum.GetName(typeof(EventLevel), value.Value!)}";
                    break;

                case nameof(EventAttribute.Keywords):
                    var keywordsValue = (EventKeywords)value.Value!;
                    foreach (var (keywordValue, fieldSymbol) in eventKeywords)
                    {
                        if (keywordsValue.HasFlag(keywordValue))
                        {
                            keywords.Add(fieldSymbol.ToDisplayString(CustomSymbolDisplayFormats.FullyQualifiedFormat));
                        }
                    }

                    break;

                default:
                    break;
            }
        }

        if (keywords.Count == 0)
        {
            keywords.Add("global::System.Diagnostics.Tracing.EventKeywords.None");
        }

        return new(id, level, keywords.ToArray());
    }

    private readonly Dictionary<string, int> _typeSize = new(StringComparer.Ordinal)
    {
        ["global::System.Boolean"] = 4,
        ["global::System.Byte"] = 1,
        ["global::System.SByte"] = 1,
        ["global::System.Char"] = 2,
        ["global::System.Int16"] = 2,
        ["global::System.UInt16"] = 2,
        ["global::System.Int32"] = 4,
        ["global::System.UInt32"] = 4,
        ["global::System.Int64"] = 8,
        ["global::System.UInt64"] = 8,
        ["global::System.Single"] = 4,
        ["global::System.Double"] = 8,
        ["global::System.DateTime"] = 8,
        ["global::System.Guid"] = 16,
        ["global::System.Decimal"] = 16
    };

    private int? GetParameterSize(string typeName)
    {
        if (!this._typeSize.TryGetValue(typeName, out var size))
        {
            return null;
        }

        return size;
    }
}
