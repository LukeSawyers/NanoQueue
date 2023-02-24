using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace NanoQueue;

/// <summary>
/// A queue backed by a persistent storage. 
/// </summary>
/// <typeparam name="T"></typeparam>
public class Queue<T> : IQueue<T>
{
    private readonly ILogger<Queue<T>> _logger;
    private readonly IStorage<T> _storage;

    private readonly ConcurrentDictionary<long, TaskCompletionSource<bool>> _completionsById = new();

    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly CancellationTokenSource _cts = new();

    public Queue(
        ILoggerFactory loggerFactory,
        IStorage<T> storage
    )
    {
        _logger = loggerFactory.CreateLogger<Queue<T>>();
        _storage = storage;
    }

    public Task<bool> SendAsync(T toSend)
    {
        var tcs = new TaskCompletionSource<bool>();
        _completionsById[_storage.NextKey] = tcs;
        _storage.Enqueue(toSend);
        _semaphore.Release();
        return tcs.Task;
    }

    public async Task<bool> TryDequeueAsync(Func<T, CancellationToken, Task<bool>> handleNext)
    {
        if (!_storage.TryGetCurrent(out var id, out var value))
        {
            return false;
        }
        
        try
        {
            var result = await handleNext(value, _cts.Token);
            if (result)
            {
                _completionsById.GetValueOrDefault(id.Value)?.SetResult(true);
                _storage.MoveNext();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending item {Id}: {Value}", id.Value, value);
            return false;
        }
    }
    
    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        foreach (var taskCompletionSource in _completionsById.Values)
        {
            taskCompletionSource.SetResult(false);
        }
    }
}