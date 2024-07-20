using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Text;
using Serilog;

namespace MergeSortTestTask.Sorter;

public class SortedFilesFilesMerger : ISortedFilesMerger, IDisposable
{
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    private readonly int _bufferSize;
    private readonly IComparer<string> _comparer;
    private readonly BulkReaderPool _bulkTextReaderPool;
    private readonly ConcurrentQueue<string> _filesToMerge = new ConcurrentQueue<string>();
    private readonly BlockingCollection<Task> _bagOfMergingTasks = new BlockingCollection<Task>();

    private long _filesMerging = 0;
    private string? _result;

    public SortedFilesFilesMerger(
        ILogger logger,
        int bufferSize,
        IComparer<string> comparer,
        BulkReaderPool bulkTextReaderPool)
    {
        _logger = logger;
        _bufferSize = bufferSize;
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        _bulkTextReaderPool = bulkTextReaderPool ?? throw new ArgumentNullException(nameof(bulkTextReaderPool));
    }

    public async Task<string> MergeFilesAsync(string[] initiallySortedFiles, CancellationToken cancellationToken)
    {
        _logger.Debug("Started merging files: {InitiallySortedFiles}", string.Join(", ", initiallySortedFiles));
        for (int i = 0; i < initiallySortedFiles.Length - 1; i += 2)
        {
            if (i + 1 == initiallySortedFiles.Length)
            {
                //odd number of files - we add one to the list to merge later
                _filesToMerge.Enqueue(initiallySortedFiles[i + 1]);
                _logger.Debug("Added {File} to merging queue 'as is'", initiallySortedFiles[i]);
                break;
            }

            var i1 = i;
            var mergingTask = Task.Factory.StartNew<Task>(
                async () => await MergeFilesAsync(initiallySortedFiles[i1], initiallySortedFiles[i1 + 1], cancellationToken)
                    .ConfigureAwait(false),
                cancellationToken,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Current);

            _bagOfMergingTasks.Add(mergingTask, cancellationToken);
            _logger.Debug(
                "Added a task to merge files {File1} and {File2}",
                initiallySortedFiles[i1],
                initiallySortedFiles[i1 + 1]);
        }

        foreach (var mergingTask in _bagOfMergingTasks.GetConsumingEnumerable(cancellationToken))
        {
            await mergingTask.ConfigureAwait(false);
        }

        _logger.Debug("Merge finished result file is {FilePath}", _result);
#pragma warning disable CS8603 //when _bagOfMergingTasks.GetConsumingEnumerable is finished, the result will have a value
        return _result;
#pragma warning restore CS8603
    }

    private async Task MergeFilesAsync(
        string firstFile,
        string secondFile,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.Debug("Started merging {firstFile} and {secondFile}", firstFile, secondFile);
            Interlocked.Increment(ref _filesMerging);
            var fs1 = File.OpenRead(firstFile);
            var fs2 = File.OpenRead(secondFile);
            var bulkTextReader1 = _bulkTextReaderPool.Get();
            var bulkTextReader2 = _bulkTextReaderPool.Get();
            var linesSequence1 = bulkTextReader1.ReadAllLinesAsync(fs1, cancellationToken);
            var linesSequence2 = bulkTextReader2.ReadAllLinesAsync(fs2, cancellationToken);

            var tempFilePath = Path.GetTempFileName();
            await using var writer = new StreamWriter(tempFilePath, append: false, Encoding.ASCII, _bufferSize);
            _logger.Verbose("Merging {FirstFile} and {SecondFile} into {MergeFile}", firstFile, secondFile, tempFilePath);
            var estimatedFileChunkSize = 0;

            await foreach (var lineInMergedSequence in MergerHelper.MergePreserveOrderAsync(
                               linesSequence1,
                               linesSequence2,
                               _comparer,
                               cancellationToken))
            {
                await writer.WriteLineAsync(lineInMergedSequence).ConfigureAwait(false);
                estimatedFileChunkSize += lineInMergedSequence.Length;
                if (estimatedFileChunkSize >= _bufferSize - sizeof(char) * 256)
                {
                    await writer.FlushAsync().ConfigureAwait(false);
                    estimatedFileChunkSize = 0;
                }
            }

            await writer.FlushAsync().ConfigureAwait(false);
            writer.Close();
            fs1.Close();
            fs2.Close();
            File.Delete(firstFile);
            File.Delete(secondFile);
            _bulkTextReaderPool.Return(bulkTextReader1);
            _bulkTextReaderPool.Return(bulkTextReader2);
            Interlocked.Decrement(ref _filesMerging);
            _logger.Verbose("Merging {firstFile} and {secondFile} into {MergeFile}", firstFile, secondFile, tempFilePath);

            var haveFileToMerge = _filesToMerge.TryDequeue(out var nextFileName);
            var filesCurrentlyMerging = Interlocked.Read(ref _filesMerging);
            if (haveFileToMerge)
            {
                var newMergingTask = Task.Factory.StartNew(
#pragma warning disable CS8604 //because _filesToMerge.TryDequeue returned true
                    async () => await MergeFilesAsync(tempFilePath, nextFileName, cancellationToken).ConfigureAwait(false),
#pragma warning restore CS8604
                    cancellationToken,
                    TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Current);
                _bagOfMergingTasks.Add(newMergingTask, cancellationToken);
                _logger.Debug(
                    "Added a task to merge files {File1} and {File2}",
                    tempFilePath,
                    nextFileName);
            }
            else
            {
                if (filesCurrentlyMerging > 0)
                {
                    _filesToMerge.Enqueue(tempFilePath);
                    _logger.Debug(
                        "Unable to find a pair to merge with {File}. Adding file back to queue",
                        tempFilePath);
                }
                else
                {
                    //this is a last file, all done
                    _bagOfMergingTasks.CompleteAdding();
                    //HACK: we are still waiting for the task to complete
                    //and for  _bagOfMergingTasks.GetConsumingEnumerable to stop
                    _result = tempFilePath;
                    _logger.Debug(
                        "Unable to find a pair to merge with {File}. All files are finished merging. Done",
                        tempFilePath);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        _bagOfMergingTasks.Dispose();
    }
}