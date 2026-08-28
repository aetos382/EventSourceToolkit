using System;
using System.Linq;

using Shouldly;

namespace Aetos.EventSourceToolkit.Tests.Common;

[TestClass]
public sealed class EnumerableExtensionsTest
{
    [TestMethod]
    public void FirstOrNullはアイテムが見つかった場合にそれを返す()
    {
        var items = new[] { 1, 2, 3, 4, 5 };

        var actual = items.FirstOrNull(static x => x == 2);

        actual.ShouldBe(2);
    }

    [TestMethod]
    public void FirstOrNullはアイテムが複数ある場合に最初の1つを返す()
    {
        var items = new[] { 1, 2, 3, 4, 5 };

        var actual = items.FirstOrNull(static x => x % 2 == 0);

        actual.ShouldBe(2);
    }

    [TestMethod]
    public void FirstOrNullはアイテムが見つからない場合にnullを返す()
    {
        var items = new[] { 1, 2, 3, 4, 5 };

        var actual = items.FirstOrNull(static x => x >= 100);

        actual.ShouldBeNull();
    }

    [TestMethod]
    public void SingleOrNullはアイテムが1つだけ見つかる場合にそれを返す()
    {
        var items = new[] { 1, 2, 3, 4, 5 };

        var actual = items.SingleOrNull(static x => x == 2);

        actual.ShouldBe(2);
    }

    [TestMethod]
    public void SingleOrNullはアイテムが複数見つかる場合に例外を投げる()
    {
        var items = new[] { 1, 2, 3, 4, 5 };

        Should.Throw<InvalidOperationException>(() => items.SingleOrNull(static x => x % 2 == 0));
    }


    [TestMethod]
    public void SingleOrNullはアイテムが見つからない場合にnullを返す()
    {
        var items = new[] { 1, 2, 3, 4, 5 };

        var actual = items.SingleOrNull(static x => x % 2 >= 100);

        actual.ShouldBeNull();
    }
}
