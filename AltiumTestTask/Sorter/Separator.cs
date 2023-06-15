using System.Collections.Concurrent;

namespace AltiumTestTask.Sorter;

public class Separator : ISortingSeparator, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    private readonly BulkReaderPool _bulkTextReaderPool;
    private readonly IComparer<string> _comparer;

    public Separator(BulkReaderPool bulkTextReaderPool, IComparer<string> comparer)
    {
        _bulkTextReaderPool = bulkTextReaderPool ?? throw new ArgumentNullException(nameof(bulkTextReaderPool));
        _comparer = comparer;
    }
    
    public async Task<string[]> SeparateAndSortAsync(FileStream fs, CancellationToken cancellationToken)
    {
        var bulkTextReader = _bulkTextReaderPool.Get();
        try
        {
            var initiallySortedFiles = new ConcurrentBag<string>();
            var bagOfSortingTasks = new ConcurrentBag<Task>();

            await foreach (var bulkOfLines in bulkTextReader
                               .ReadAllLinesBulkAsync(fs, cancellationToken)
                               .WithCancellation(cancellationToken))
            {
                var task = Task.Factory.StartNew(
                    () =>
                    {
                        _semaphore.Wait(cancellationToken);
                        try
                        {
                            Array.Sort(bulkOfLines, _comparer);
                            var filename = Path.GetTempFileName();
                            File.WriteAllLines(filename, bulkOfLines);
                            // ReSharper disable once AccessToDisposedClosure (because we are awaiting all tasks below)
                            initiallySortedFiles.Add(filename);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    },
                    cancellationToken,
                    TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);

                bagOfSortingTasks.Add(task);
            }

            await Task.WhenAll(bagOfSortingTasks).ConfigureAwait(false);
        
            return initiallySortedFiles.ToArray();
        }
        finally
        {
            _bulkTextReaderPool.Return(bulkTextReader);
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}