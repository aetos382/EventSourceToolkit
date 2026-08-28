using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aetos.EventSourceToolkit;

/// <summary>partial パートの追加を妨げている型宣言と、その理由を表します。</summary>
/// <param name="Declaration">妨げている型宣言。対象の型自身か、それを包含する型のいずれか。</param>
/// <param name="Reason">妨げている理由。</param>
internal sealed record AugmentationBlocker(
    TypeDeclarationSyntax Declaration,
    AugmentationBlockerReason Reason);
