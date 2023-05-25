using System.Runtime.CompilerServices;

namespace AltiumTestTask.Sorter;

public static class MergerHelper
{
    /// <summary>
    ///     Merges 2 <see cref="IAsyncEnumerable{T}"/> <paramref name="aList"/> amd <paramref name="bList"/>
    /// with a comparer <paramref name="comparer"/>. Order of the original sequences will be preserved.
    /// </summary>
    /// <param name="aList">An <see cref="IAsyncEnumerable{T}"/> to merge. (Longest one preferably).</param>
    /// <param name="bList">An <see cref="IAsyncEnumerable{T}"/> to merge. (Shortest one preferably).</param>
    /// <param name="comparer">Comparer to </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that may be used to stop processing of the task.</param>
    /// <typeparam name="T">Type of the enumerable.</typeparam>
    /// <returns>Merged <see cref="IAsyncEnumerable{T}"/> with order of the original sequences preserved.</returns>
    public static async IAsyncEnumerable<T> MergeAndPreserveOrderAsync<T>(
        IAsyncEnumerable<T> aList,
        IAsyncEnumerable<T> bList,
        IComparer<T> comparer,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var a = aList.GetAsyncEnumerator(cancellationToken);
        var aHasItems = await a.MoveNextAsync();

        await foreach (var b in bList.WithCancellation(cancellationToken))
        {
            while (aHasItems && comparer.Compare(a.Current,b) <= 0)
            {
                yield return a.Current; aHasItems = await a.MoveNextAsync();
            }
            yield return b;
        }
        // And anything left in a
        while (aHasItems)
        {
            yield return a.Current;
            aHasItems = await a.MoveNextAsync();
        }
    }
    
    /// <summary>
    ///     Merges 2 <see cref="IEnumerable{T}"/> <paramref name="aList"/> amd <paramref name="bList"/>
    /// with a comparer <paramref name="comparer"/>. Order of the original sequences will be preserved.
    /// </summary>
    /// <param name="aList">An <see cref="IEnumerable{T}"/> to merge. (Longest one preferably).</param>
    /// <param name="bList">An <see cref="IEnumerable{T}"/> to merge. (Shortest one preferably).</param>
    /// <param name="comparer">Comparer to </param>
    /// <typeparam name="T">Type of the enumerable.</typeparam>
    /// <returns>Merged <see cref="IAsyncEnumerable{T}"/> with order of the original sequences preserved.</returns>
    public static IEnumerable<T> MergeAndPreserveOrder<T>(
        IEnumerable<T> aList,
        IEnumerable<T> bList,
        IComparer<T> comparer)
    {
        using var a = aList.GetEnumerator();
        var aHasItems = a.MoveNext();

        foreach (var b in bList)
        {
            while (aHasItems && comparer.Compare(a.Current,b) <= 0)
            {
                yield return a.Current; aHasItems = a.MoveNext();
            }
            yield return b;
        }
        // And anything left in a
        while (aHasItems)
        {
            yield return a.Current;
            aHasItems = a.MoveNext();
        }
    }
}