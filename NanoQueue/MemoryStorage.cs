using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace NanoQueue;

/// <summary>
/// A dastardly storage implementation that uses an In-Memory ConcurrentQueue. NOT PERSISTENT.
/// </summary>
/// <typeparam name="T"></typeparam>
public class MemoryStorage<T> : IStorage<T>
{
    public long NextKey { get; private set; } = 1;

    private readonly ConcurrentQueue<(long, T)> _queue = new();

    public void Enqueue(T obj)
    {
        _queue.Enqueue((NextKey, obj));
        NextKey++;
    }

    public bool TryGetCurrent([NotNullWhen(true)] out long? id, [NotNullWhen(true)] out T? value)
    {
        if (_queue.TryPeek(out var next))
        {
            id = next.Item1;
            value = next.Item2;
            return true;
        }

        id = null;
        value = default;
        return false;
    }

    public void MoveNext()
    {
        _queue.TryDequeue(out _);
    }
}