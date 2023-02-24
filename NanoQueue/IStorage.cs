using System.Diagnostics.CodeAnalysis;

namespace NanoQueue;

public interface IStorage<T>
{
    /// <summary>
    /// Guaranteed to be the key of the next item added.
    /// </summary>
    long NextKey { get; }

    /// <summary>
    /// Enqueue a new item in the storage. 
    /// </summary>
    /// <param name="obj"></param>
    void Enqueue(T obj);

    /// <summary>
    /// Try to get the current item in the queue. Does not change the current item.
    /// </summary>
    /// <param name="id">The id of the value</param>
    /// <param name="value">The value to send</param>
    /// <returns>True if there was an item to dequeue, else false.</returns>
    bool TryGetCurrent([NotNullWhen(true)] out long? id, [NotNullWhen(true)] out T? value);

    /// <summary>
    /// Advance to the next item. 
    /// </summary>
    void MoveNext();
}