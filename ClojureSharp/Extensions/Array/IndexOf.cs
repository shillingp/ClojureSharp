namespace ClojureSharp.Extensions.Array;

public static partial class ArrayExtensions
{
    public static int IndexOf<TSource>(this TSource[] source, Func<TSource, bool> predicate)
    {
        int index = 0;
        foreach (TSource item in source.AsSpan())
        {
            if (predicate.Invoke(item))
                return index;
        
            index++;
        }
        
        return -1;
    }
}
