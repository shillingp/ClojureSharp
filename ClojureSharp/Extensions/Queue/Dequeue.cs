using System.Buffers;

namespace ClojureSharp.Extensions.Queue;

public static partial class QueueExtensions
{
    public static T[] Dequeue<T>(this Queue<T> queue, int numberOfItemsToDequeue)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfItemsToDequeue);
        
        T[] returnArray = ArrayPool<T>.Shared.Rent(numberOfItemsToDequeue);
        
        for (int i = 0; i < numberOfItemsToDequeue; i++)
            returnArray[i] = queue.Dequeue();
        
        ArrayPool<T>.Shared.Return(returnArray);
        return returnArray[..numberOfItemsToDequeue];
    }
}