using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Aetos.EventSourceToolkit.SourceGenerators.Models;

namespace Aetos.EventSourceToolkit.SourceGenerators;

internal sealed class EventSourceParser
{
    private readonly SemanticModel _semanticModel;
    private readonly WellKnownSymbols _wellKnownSymbols;

    public EventSourceParser(
        SemanticModel semanticModel)
    {
        ArgumentNullException.ThrowIfNull(semanticModel);

        this._semanticModel = semanticModel;
        this._wellKnownSymbols = new WellKnownSymbols(semanticModel.Compilation);
    }

    public EventSourceMethodInfo? ParseEventSource(
        MethodDeclarationSyntax syntaxNode,
        IMethodSymbol symbol,
        CancellationToken cancellationToken)
    {
        var containingType = symbol.ContainingType;
        var wellKnownSymbols = this._wellKnownSymbols;

        // メソッドが static → 無視, 生成対象外
        if (symbol.IsStatic)
        {
            return null;
        }

        // メソッドを含むクラスに GeneratedEventSourceAttribute がついていない → 無視, 生成対象外
        var markerAttribute = containingType.GetAttribute(wellKnownSymbols.GeneratedEventSourceAttribute);
        if (markerAttribute is null)
        {
            return null;
        }

        // メソッドを含むクラスが（間接的に）EventSource から派生していない → 警告, 生成対象外
        if (!this.IsDerivedFromEventSource(containingType))
        {
            return null;
        }

        // メソッドを含むクラスに EventSourceAttribute がついていない → 警告, 生成対象外
        if (this.GetEventSourceAttribute(containingType) is null)
        {
            return null;
        }

        // メソッドを含むクラス（またはそれを包含する型）に partial パートを追加できない → 無視, 生成対象外
        if (syntaxNode.Parent is not TypeDeclarationSyntax containingTypeNode ||
            !containingTypeNode.CanBeAugmented)
        {
            return null;
        }

        // メソッドが partial でない → 無視, 生成対象外
        if (!syntaxNode.HasPartialModifier)
        {
            return null;
        }

        // メソッドの実装が存在する → 無視, 生成対象外
        if (symbol.PartialImplementationPart is not null)
        {
            return null;
        }

        // メソッドの戻り値が void でない → 無視, 生成対象外
        if (!syntaxNode.ReturnsVoid)
        {
            return null;
        }

        var keywordsType = containingType.GetTypeMembers("Keywords").SingleOrDefault();

        var eventMetadata = this.ParseEventAttribute(
            symbol.GetAttribute(wellKnownSymbols.EventAttribute)!,
            keywordsType);

        var parameterList = syntaxNode.ParameterList.Parameters;
        var parameters = new List<EventSourceMethodParameterInfo>(parameterList.Count);

        var semanticModel = this._semanticModel;
        var comparer = SymbolEqualityComparer.Default;
        var hasRelatedActivityIdParameter = false;

        foreach (var (index, parameter) in parameterList.Index())
        {
            var parameterName = parameter.Identifier.ValueText;
            var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken)!;
            var parameterTypeSymbol = parameterSymbol.Type;

            if (index == 0 &&
                string.Equals(parameterName, "relatedActivityId", StringComparison.OrdinalIgnoreCase) &&
                comparer.Equals(parameterTypeSymbol, wellKnownSymbols.Guid))
            {
                hasRelatedActivityIdParameter = true;
                continue;
            }

            // パラメーターの型がサポートされていない → 無視, 生成対象外（診断は Analyzer 側が行う）
            if (!EventSourceUtilities.IsSupportedParameterType(parameterTypeSymbol, wellKnownSymbols))
            {
                return null;
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

            var parameterInfo = new EventSourceMethodParameterInfo(parameterTypeName, parameter.Identifier.ValueText, isEnum, size);
            parameters.Add(parameterInfo);
        }

        var containingClass = (ClassDeclarationSyntax)syntaxNode.Parent!;
        var containingClassName = containingClass.Identifier.ValueText;

        var ancestorTypes = new List<ContainingTypeInfo>();
        var namespaceSegments = new List<string>();

        var parentNode = containingClass.Parent;
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
            containingClassName,
            syntaxNode.AccessibilityKeyword,
            syntaxNode.Identifier.Text,
            eventMetadata,
            parameters.ToArray(),
            hasRelatedActivityIdParameter);

        return methodInfo;
    }

    private AttributeData? GetEventSourceAttribute(INamedTypeSymbol type)
    {
        return type.GetAttribute(this._wellKnownSymbols.EventSourceAttribute);
    }

    private bool IsDerivedFromEventSource(INamedTypeSymbol type)
    {
        return type.IsDerivedFrom(this._wellKnownSymbols.EventSource);
    }

    private EventMetadataInfo ParseEventAttribute(
        AttributeData data,
        INamedTypeSymbol? keywordsTypeSymbol)
    {
        var wellKnownSymbols = this._wellKnownSymbols;
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
                .Where(x => comparer.Equals(x.Type, wellKnownSymbols.EventKeywords))
                .Select(static x => (Keyword: (EventKeywords)x.ConstantValue!, Symbol: x))
                .ToArray();
        }

        var ctorArgs = data.ConstructorArguments;
        if (ctorArgs.Length == 1)
        {
            var idArg = ctorArgs[0];
            if (idArg.Kind == TypedConstantKind.Primitive && comparer.Equals(idArg.Type, wellKnownSymbols.Int32) && idArg.Value is int idValue)
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
                    level = Enum.GetName(typeof(EventLevel), value.Value!);
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

        level = $"global::System.Diagnostics.Tracing.EventLevel.{level}";

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
