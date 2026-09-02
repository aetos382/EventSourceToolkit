using System;
using System.Collections.Generic;

namespace Aetos.EventSourceToolkit;

public static partial class EnumerableExtensions
{
    public static T? FirstOrNull<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in source)
        {
            if (predicate(item))
            {
                return item;
            }
        }

        return null;
    }

    public static T? SingleOrNull<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        T? found = null;

        foreach (var item in source)
        {
            if (predicate(item))
            {
                if (found is not null)
                {
                    // TODO: message
                    throw new InvalidOperationException();
                }

                found = item;
            }
        }

        return found;
    }
}
