using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClojureSharp.Extensions.Span;

public static partial class SpanExtensions
{
    public static int LastIndexOf<TSource>(this ReadOnlySpan<TSource> source, Func<TSource, bool> predicate)
    {
        ref TSource searchSpace = ref MemoryMarshal.GetReference(source);

        for (int i = source.Length - 1; i >= 0; i--)
            if (predicate(Unsafe.Add(ref searchSpace, i)))
                return i;

        return -1;
    }
}