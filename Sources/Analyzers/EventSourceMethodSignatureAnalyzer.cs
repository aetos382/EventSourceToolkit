using System;
using System.Collections.Immutable;

using Aetos.EventSourceToolkit.Analyzers.Properties;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Aetos.EventSourceToolkit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventSourceMethodSignatureAnalyzer :
    DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor ParameterTypeNotSupported = new(
        DiagnosticIds.ParameterTypeNotSupported,
        Resources.GetLocalizableResourceString(nameof(Resources.ParameterTypeNotSupportedTitle)),
        Resources.GetLocalizableResourceString(nameof(Resources.ParameterTypeNotSupportedMessage)),
        DiagnosticCategories.General,
        DiagnosticSeverity.Error,
        true,
        Resources.GetLocalizableResourceString(nameof(Resources.ParameterTypeNotSupportedDescription)),
        DiagnosticHelpLinks.GetHelpLinkUri(DiagnosticIds.ParameterTypeNotSupported));

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.MethodDeclaration);
    }

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        ParameterTypeNotSupported
    ];

    private static void SyntaxNodeAction(
        SyntaxNodeAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var compilation = context.Compilation;
        var semanticModel = context.SemanticModel;
        var wellKnownTypes = new WellKnownSymbols(compilation);

        var node = (MethodDeclarationSyntax)context.Node;
        var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

        if (symbol is null)
        {
            return;
        }

        // [NonEvent] がついているメソッドは検査対象外
        if (symbol.HasAttribute(wellKnownTypes.NonEventAttribute))
        {
            return;
        }

        // [Event] がついていないメソッドは付けるといいよ
        var eventAttribute = symbol.GetAttribute(wellKnownTypes.EventAttribute);
        if (eventAttribute is null)
        {
            // TODO: Diagnostics
        }
        else
        {
            if (symbol.IsStatic)
            {
                // [Event] がついているなら static メソッドはダメよ
                // TODO: diagnostic
            }
        }

        var parameters = symbol.Parameters;
        foreach (var parameter in parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parameterType = parameter.Type;

            if (!IsSupportedParameterType(parameterType, wellKnownTypes))
            {
                // TODO: diagnostic
                continue;
            }
        }
    }

    private static bool IsSupportedParameterType(
        ITypeSymbol typeSymbol,
        WellKnownSymbols wellKnownTypes)
    {
        var typeKind = typeSymbol.TypeKind;
        var specialType = typeSymbol.SpecialType;

        if (specialType is not SpecialType.None)
        {
            if (specialType is (
                SpecialType.System_Boolean or
                SpecialType.System_Byte or
                SpecialType.System_Char or
                SpecialType.System_DateTime or
                SpecialType.System_Decimal or
                SpecialType.System_Double or
                SpecialType.System_Int16 or
                SpecialType.System_Int32 or
                SpecialType.System_Int64 or
                SpecialType.System_IntPtr or
                SpecialType.System_SByte or
                SpecialType.System_Single or
                SpecialType.System_String or
                SpecialType.System_UInt16 or
                SpecialType.System_UInt32 or
                SpecialType.System_UInt64))
            {
                return true;
            }
        }

        if (typeKind is TypeKind.Enum)
        {
            if (typeSymbol is INamedTypeSymbol { EnumUnderlyingType: { } enumUnderlyingType })
            {
                if (enumUnderlyingType.SpecialType is (
                    SpecialType.System_Byte or
                    SpecialType.System_Int16 or
                    SpecialType.System_Int32 or
                    SpecialType.System_Int64 or
                    SpecialType.System_SByte or
                    SpecialType.System_UInt16 or
                    SpecialType.System_UInt32 or
                    SpecialType.System_UInt64))
                {
                    return true;
                }
            }
        }

        if (typeKind is TypeKind.Array)
        {
            if (typeSymbol is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
            {
                return true;
            }
        }

        var comparer = SymbolEqualityComparer.Default;
        if (comparer.Equals(typeSymbol, wellKnownTypes.Guid))
        {
            return true;
        }

        return false;
    }
}
