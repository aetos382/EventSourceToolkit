using Aetos.Tracing.Diagnostics;
using Aetos.Tracing.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;

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

    public EventSourceMethodInfoWithDiagnostics ParseEventSourceMethod(
        MethodDeclarationSyntax syntaxNode,
        IMethodSymbol symbol,
        CancellationToken cancellationToken)
    {
        var containingType = symbol.ContainingType;
        var wellKnownTypes = this._wellKnownTypes;
        var diagnostics = new List<DiagnosticInfo>();

        // そのメソッドを含むクラスに GeneratedEventSourceAttribute がついているか → 無視, 生成対象外
        var markerAttribute = containingType.GetAttribute(wellKnownTypes.GeneratedEventSourceAttribute);
        if (markerAttribute is null)
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        // そのメソッドを含むクラスが（間接的に）EventSource から派生しているか → 警告, 生成対象外
        if (!this.IsDerivedFromEventSource(containingType))
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        // そのメソッドを含むクラスに EventSourceAttribute がついているか → 警告, 生成対象外
        if (this.GetEventSourceAttribute(containingType) is null)
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        // クラスに対する警告はメソッド単位でのコード生成では大変なので、別の Analyzer を用意する

        // そのメソッドは partial か → 無視, 生成対象外
        if (!syntaxNode.HasPartialModifier)
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        // そのメソッドの実装が存在しないか → 無視, 生成対象外
        if (symbol.PartialImplementationPart is not null)
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        // そのメソッドの戻り値は void か → 無視, 生成対象外
        if (!syntaxNode.ReturnsVoid)
        {
            return EventSourceMethodInfoWithDiagnostics.Empty;
        }

        Dictionary<string, EventKeywords> keywords = [];

        var keywordsType = containingType.GetTypeMembers("Keywords").SingleOrDefault();

        var eventMetadata = this.ParseEventAttribute(
            symbol.GetAttribute(wellKnownTypes.EventAttribute)!,
            keywordsType);

        var parameterList = syntaxNode.ParameterList.Parameters;
        var parameters = new List<EventSourceMethodParameterInfo>(parameterList.Count);

        var semanticModel = this._semanticModel;
        var supportedTypes = new SupportedTypes(wellKnownTypes);

        foreach (var parameter in parameterList)
        {
            var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken)!;
            var parameterTypeSymbol = parameterSymbol.Type;

            if (!supportedTypes.IsSupported(parameterTypeSymbol))
            {
                // そのメソッドのパラメーターはサポートされているか
                diagnostics.Add(new DiagnosticInfo(DiagnosticIds.ParameterTypeNotSupported, parameter.GetNodeLocationInfo()));
            }

            // SymbolDisplayFormat では SymbolDisplayMiscellaneousOptions.UseSpecialTypes フラグが含まれていなくても
            // IntPtr / UIntPtr は "nint" / "nuint" になってしまうので、自力で文字列化する。
            // https://github.com/dotnet/roslyn/issues/76895

            // なお UIntPtr はイベント メソッドのパラメーター型としてサポートされない。
            var parameterTypeName = parameterTypeSymbol.IsNativeIntegerType
                ? parameterTypeSymbol.SpecialType == SpecialType.System_IntPtr
                    ? "global::System.IntPtr"
                    : "global::System.UIntPtr"
                : parameterTypeSymbol.ToDisplayString(CustomSymbolDisplayFormats.FullyQualifiedFormat);

            var parameterInfo = new EventSourceMethodParameterInfo(parameterTypeName, parameter.Identifier.Text);
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

        return new(id, level, keywords.ToArray());
    }
}
