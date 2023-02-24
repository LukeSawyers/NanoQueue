using Microsoft.Extensions.Logging;

namespace NanoQueue;

/// <summary>
/// A queue with a constructor registered handler that automatically handles queued items. 
/// </summary>
/// <typeparam name="T"></typeparam>
public class AutoQueue<T> : IAutoHandleQueue<T>
{
    private readonly ILogger _logger;
    private readonly IQueue<T> _queue;
    private readonly Func<T, CancellationToken, Task<bool>> _handleFunction;

    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _sendLoopTask;

    public AutoQueue(
        ILoggerFactory loggerFactory,
        IQueue<T> queue,
        Func<T, CancellationToken, Task<bool>> handleFunction
    )
    {
        _logger = loggerFactory.CreateLogger<AutoQueue<T>>();
        _queue = queue;
        _handleFunction = handleFunction;
        _sendLoopTask = RunLoop();
    }

    private async Task RunLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                while (await _queue.TryDequeueAsync((next, _) => _handleFunction(next, _cts.Token)))
                {
                }
                
                await _semaphore.WaitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in send loop");
            }
        }
    }

    public Task<bool> SendAsync(T toSend)
    {
        var task = _queue.SendAsync(toSend);
        _semaphore.Release();
        return task;
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _sendLoopTask;
        await _queue.DisposeAsync();
    }
}