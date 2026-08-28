using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Aetos.Tracing.Models;

[CollectionBuilder(typeof(EquatableArray), nameof(EquatableArray.Create))]
internal readonly struct EquatableArray<T> :
    IEquatable<EquatableArray<T>>,
    IReadOnlyList<T>
{
    private readonly T[] _array;

    public EquatableArray(ReadOnlySpan<T> source)
    {
        this._array = source.ToArray();
    }

    /// <inheritdoc />
    public bool Equals(EquatableArray<T> other)
    {
        return ((IStructuralEquatable)this._array).Equals(other._array, EqualityComparer<T>.Default);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && this.Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var array = this._array;
        if (array.Length == 0)
        {
            return 0;
        }

        var comparer = EqualityComparer<T>.Default;
        var hash = 0;

        foreach (var item in array)
        {
            hash = unchecked((hash * 31) + comparer.GetHashCode(item));
        }

        return hash;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => this._array.AsEnumerable().GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => this._array.GetEnumerator();

    /// <inheritdoc />
    public int Count => this._array.Length;

    /// <inheritdoc />
    public T this[int index] => this._array[index];

    public static implicit operator EquatableArray<T>(ReadOnlySpan<T> source) => new(source);
}

internal static class EquatableArray
{
    public static EquatableArray<T> Create<T>(ReadOnlySpan<T> source)
    {
        return new(source);
    }
}
