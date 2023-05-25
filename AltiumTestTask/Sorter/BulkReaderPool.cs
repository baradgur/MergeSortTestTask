using System.Collections.Concurrent;

namespace AltiumTestTask.Sorter;

// public class ObjectPool<T>
// {
//     private readonly ConcurrentBag<T> _objects;
//     private readonly Func<T> _objectGenerator;
//
//     public ObjectPool(Func<T> objectGenerator)
//     {
//         _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
//         _objects = new ConcurrentBag<T>();
//     }
//
//     public T Get() => _objects.TryTake(out var item) ? item : _objectGenerator();
//
//     public void Return(T item)
//     {
//         item.Reset()
//         _objects.Add(item);
//     }
// }


// public class BulkReaderPool
// {
//     private readonly ConcurrentBag<BulkTextReader> _objects;
//     private readonly Func<BulkTextReader> _objectGenerator;
//
//     public BulkReaderPool(Func<BulkTextReader> objectGenerator)
//     {
//         _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
//         _objects = new ConcurrentBag<BulkTextReader>();
//     }
//
//     public BulkTextReader Get() => _objects.TryTake(out var item) ? item : _objectGenerator();
//
//     public void Return(BulkTextReader item)
//     {
//         item.Reset();
//         _objects.Add(item);
//     }
// }