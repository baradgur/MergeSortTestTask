using System.Collections.Concurrent;

namespace MergeSortTestTask.Sorter;

public class BulkReaderPool
{
    private readonly ConcurrentBag<BulkTextReader> _objects;
    private readonly Func<BulkTextReader> _objectGenerator;

    public BulkReaderPool(Func<BulkTextReader> objectGenerator)
    {
        _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        _objects = new ConcurrentBag<BulkTextReader>();
    }

    public BulkTextReader Get() => _objects.TryTake(out var item) ? item : _objectGenerator();

    public void Return(BulkTextReader item)
    {
        item.Reset();
        _objects.Add(item);
    }
}
