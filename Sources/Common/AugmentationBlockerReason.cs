namespace Aetos.EventSourceToolkit;

/// <summary>partial パートを追加できない理由を表します。</summary>
public enum AugmentationBlockerReason
{
    /// <summary>partial 修飾子を持たない。</summary>
    NotPartial,

    /// <summary>file ローカル型である。</summary>
    FileLocal
}
