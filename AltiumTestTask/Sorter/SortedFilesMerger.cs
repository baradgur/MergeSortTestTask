using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using Serilog;

namespace AltiumTestTask.Sorter;

public class SortedFilesFilesMerger : ISortedFilesMerger, IDisposable
{
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    private readonly int _bufferSize;
    private readonly IComparer<ReadOnlyMemory<char>> _comparer;
    private readonly PairQueue<string> _filesToMergeQueue = new PairQueue<string>();
    private readonly BlockingCollection<Task> _bagOfMergingTasks = new BlockingCollection<Task>();
    private readonly ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();

    private long _filesToMerge = 0;
    private string? _result;

    public SortedFilesFilesMerger(
        ILogger logger,
        int bufferSize,
        IComparer<ReadOnlyMemory<char>> comparer)
    {
        _logger = logger;
        _bufferSize = bufferSize;
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
    }

    public async Task<string> MergeFilesAsync(string[] initiallySortedFiles, CancellationToken cancellationToken)
    {
        _logger.Debug("Started merging files: {InitiallySortedFiles}", string.Join(", ", initiallySortedFiles));
        Interlocked.Add(ref _filesToMerge, initiallySortedFiles.Length);
        for (int i = 0; i < initiallySortedFiles.Length - 1; i += 2)
        {
            if (i + 1 == initiallySortedFiles.Length)
            {
                //odd number of files - we add one to the list to merge later
                _filesToMergeQueue.Enqueue(initiallySortedFiles[i + 1]);
                _logger.Debug("Added {File} to merging queue 'as is'", initiallySortedFiles[i]);
                break;
            }

            // _semaphore will be released after files will be merged.
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            StartMerging(initiallySortedFiles[i], initiallySortedFiles[i + 1], cancellationToken);
        }

        while (true)
        {
            await Task.Delay(10, cancellationToken);
            if (_filesToMergeQueue.TryDequeuePair(out var result1, out var result2))
            {
                StartMerging(result1, result2, cancellationToken);
            }
            else
            {
                if (Interlocked.Read(ref _filesToMerge) == 1)
                {
                    return _filesToMergeQueue.GetOne();
                }
            }
        }
    }

    private void StartMerging(string file1, string file2, CancellationToken cancellationToken)
    {
        var mergingTask = Task.Factory.StartNew<Task>(
            async () =>
            {
                try
                {
                    var newMergedFile = await MergeFilesAsync(
                            file1,
                            file2,
                            cancellationToken)
                        .ConfigureAwait(false);
                    _filesToMergeQueue.Enqueue(newMergedFile);
                    Interlocked.Add(ref _filesToMerge, -1);
                }
                finally
                {
                    _semaphore.Release();
                }
            },
            cancellationToken,
            TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Current);

        _tasks.Add(mergingTask);
        _logger.Debug(
            "Added a task to merge files {File1} and {File2}",
            file1,
            file2);
    }

    private async Task<string> MergeFilesAsync(
        string firstFile,
        string secondFile,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.Debug("Started merging {File1} and {File2}", firstFile, secondFile);
            Interlocked.Increment(ref _filesToMerge);
            var fs1 = File.OpenRead(firstFile);
            var fs2 = File.OpenRead(secondFile);

            var tempFilePath = Path.GetTempFileName();
            await using var writer = new StreamWriter(tempFilePath, append: false, Encoding.ASCII, _bufferSize);
            _logger.Verbose("Merging {FirstFile} and {SecondFile} into {MergeFile}", firstFile, secondFile, tempFilePath);

            using var reader1 = new StreamReader(fs1, detectEncodingFromByteOrderMarks: false, bufferSize: _bufferSize);
            using var reader2 = new StreamReader(fs1, detectEncodingFromByteOrderMarks: false, bufferSize: _bufferSize);

            while (true)
            {
                using var buffer1Owner1 = MemoryPool<char>.Shared.Rent(_bufferSize);
                var buffer1 = buffer1Owner1.Memory;
                int bytesRead1 = 0, bytesRead2 = 0;
                if (!reader1.EndOfStream)
                {
                    bytesRead1 = await reader1.ReadAsync(buffer1, cancellationToken);
                }

                using var buffer1Owner2 = MemoryPool<char>.Shared.Rent(_bufferSize);
                var buffer2 = buffer1Owner2.Memory;
                if (!reader2.EndOfStream)
                {
                    bytesRead2 = await reader2.ReadAsync(buffer2, cancellationToken);
                }

                if (bytesRead1 == 0)
                {
                    while ((bytesRead2 = await reader2.ReadAsync(buffer2, cancellationToken)) > 0)
                    {
                        await writer.WriteLineAsync(buffer2[..bytesRead2], cancellationToken).ConfigureAwait(false);
                    }

                    await writer.FlushAsync().ConfigureAwait(false);
                    break;
                }

                if (bytesRead2 == 0)
                {
                    while ((bytesRead1 = await reader1.ReadAsync(buffer1, cancellationToken)) > 0)
                    {
                        await writer.WriteLineAsync(buffer1[..bytesRead1], cancellationToken).ConfigureAwait(false);
                    }

                    await writer.FlushAsync().ConfigureAwait(false);
                    break;
                }
                //at this point we have both buffer containing of data from both files.

                var actualData1 = buffer1[..bytesRead1];
                var actualData2 = buffer2[..bytesRead1];
                var lastLineEnd1 = actualData1.Span.LastIndexOf('\n');
                var lastLineEnd2 = actualData2.Span.LastIndexOf('\n');
                ReadOnlyMemory<char> activeSlice1 = lastLineEnd1 > 0 ? actualData1[..lastLineEnd1] : actualData1;
                ReadOnlyMemory<char> activeSlice2 = lastLineEnd2 > 0 ? actualData2[..lastLineEnd2] : actualData1;

                var linesSequence1 = activeSlice1.SeparateToLines();
                var linesSequence2 = activeSlice2.SeparateToLines();

                foreach (var lineInMergedSequence in MergerHelper.MergeAndPreserveOrder(
                             linesSequence1,
                             linesSequence2,
                             _comparer))
                {
                    await writer.WriteLineAsync(lineInMergedSequence, cancellationToken).ConfigureAwait(false);
                    await writer.WriteLineAsync(lineInMergedSequence, cancellationToken).ConfigureAwait(false);
                }

                //go back to that start of the line
                fs1.Seek(-1 * (actualData1.Length - lastLineEnd1), SeekOrigin.Current);
                fs2.Seek(-1 * (actualData2.Length - lastLineEnd2), SeekOrigin.Current);
            }

            await writer.FlushAsync().ConfigureAwait(false);
            writer.Close();
            fs1.Close();
            fs2.Close();
            File.Delete(firstFile);
            File.Delete(secondFile);

            return tempFilePath;
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