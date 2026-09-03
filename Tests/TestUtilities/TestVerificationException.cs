using System;

namespace Aetos.EventSourceToolkit.Tests.TestUtilities;

/// <summary>
/// 期待値との比較に失敗したことを表します。
/// テストのセットアップの誤り（<see cref="InvalidOperationException" />）とは区別する。
/// </summary>
public sealed class TestVerificationException :
    Exception
{
    public TestVerificationException()
    {
    }

    public TestVerificationException(
        string message)
        : base(message)
    {
    }

    public TestVerificationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
