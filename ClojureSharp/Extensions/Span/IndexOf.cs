using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClojureSharp.Extensions.Span;

public static partial class SpanExtensions
{
    public static int IndexOf<TSource>(
        this ReadOnlySpan<TSource> source,
        Func<TSource, bool> predicate,
        int startIndex = 0)
    {
        ref TSource searchSpace = ref MemoryMarshal.GetReference(source);
        
        for (int i = startIndex; i < source.Length; i++)
            if (predicate(Unsafe.Add(ref searchSpace, i)))
                return i;

        return -1;
    }
}
