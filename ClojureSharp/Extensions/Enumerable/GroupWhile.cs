namespace ClojureSharp.Extensions.Enumerable;

public static partial class EnumerableExtensions
{
    /// <summary>
    /// Groups sequential numbers using provided <see cref="Func{T1, T2, TResult}"/>
    /// <br/><br/>
    /// <code>
    /// int[] exampleNumbers = [1, 2, 2, 2, 1, 2, 3, 3, 1];
    /// 
    /// IEnumerable&lt;IEnumerable&lt;int&gt;&gt; groupedSequentialNumbers = exampleNumbers
    ///     .GroupWhile((previousNumber, nextNumber) => previousNumber == currentNumber);
    /// 
    /// groupedSequentialNumbers.Select(group => group.First())
    /// //  ->  [1, 2, 1, 2, 3, 1]
    /// 
    /// groupedSequentialNumbers.Select(group => group.Count())
    /// //  ->  [1, 3, 1, 1, 2, 1]
    /// </code>
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    public static IEnumerable<IEnumerable<TSource>> GroupWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, bool> condition)
    {
        TSource previousItem = source.First();
        Queue<TSource> currentGroupList = new Queue<TSource>();
        currentGroupList.Enqueue(previousItem);

        foreach (TSource currentItem in source.Skip(1))
        {
            if (condition(previousItem, currentItem) == false)
            {
                yield return currentGroupList;
                currentGroupList = new Queue<TSource>();
            }
            currentGroupList.Enqueue(currentItem);
            previousItem = currentItem;
        }

        yield return currentGroupList;
    }
}