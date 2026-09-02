namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

/// <summary>ジェネレーターが生成したソース 1 件を表します。</summary>
/// <param name="FileName">ヒント名。<see cref="FilePath" /> のファイル名部分。</param>
/// <param name="FilePath">生成されたツリーのパス。ジェネレーターごとに異なるディレクトリになる。</param>
/// <param name="Text">生成されたソース テキスト。</param>
public sealed record GeneratedSourceFile(
    string FileName,
    string FilePath,
    string Text);
