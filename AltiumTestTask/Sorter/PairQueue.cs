using System.Diagnostics.CodeAnalysis;

namespace AltiumTestTask.Sorter;

public class PairQueue<T>
{
    private readonly object _lock = new object();
    private readonly Queue<T> _queue = new Queue<T>();

    public void Enqueue(T x)
    {
        lock (_lock)
        {
            _queue.Enqueue(x);
        }
    }
    
    /// <summary>
    /// Try to get 2 elements from queue. If queue contains less than 2 elements returns false.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public bool TryDequeuePair([NotNullWhen(true)]out T? x,[NotNullWhen(true)] out T? y)
    {
        lock (_lock)
        {
            if (_queue.Count < 2)
            {
                x = default;
                y = default;
                return false;
            }

            x = _queue.Dequeue();
            y = _queue.Dequeue();
            return true;
        }
    }
    
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    /// <summary>
    /// Returns the last element from queue.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public T GetOne()
    {
        lock (_lock)
        {
            if (_queue.Count != 0)
            {
                throw new InvalidOperationException();
            }

            return _queue.Dequeue();
        }
    }
}