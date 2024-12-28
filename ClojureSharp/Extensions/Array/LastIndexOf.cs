using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClojureSharp.Extensions.Array;

public static partial class ArrayExtensions
{
    public static int LastIndexOf<TSource>(this TSource[] source, Func<TSource, bool> predicate)
    {
        Span<TSource> sourceSpan = source.AsSpan();
        ref TSource searchSpace = ref MemoryMarshal.GetReference(sourceSpan);

        for (int i = sourceSpan.Length - 1; i >= 0; i--)
            if (predicate(Unsafe.Add(ref searchSpace, i)))
                return i;

        return -1;
    }
}