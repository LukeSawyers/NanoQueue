namespace NanoQueue;

public interface ISendQueue<T> : IAsyncDisposable, IDisposable
{
    Task<bool> SendAsync(T toSend);
}

public interface IAutoHandleQueue<T> : ISendQueue<T>
{
}

public interface IQueue<T> : ISendQueue<T>
{
    Task<bool> TryDequeueAsync(Func<T, CancellationToken, Task<bool>> handleNext);
}