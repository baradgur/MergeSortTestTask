using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using Serilog;

namespace AltiumTestTask.Sorter;

public class LargeFileSeparatorSorter : ILargeFileSeparatorSorter, IDisposable
{
    public static readonly int MaxDegreeOfParallelism = Environment.ProcessorCount;

    private const int MinBufferSize = 128; //equals StreamReader.MinBufferSize

    /// <summary>
    /// Estimate of the amount of lines in reading buffer
    /// </summary>
    private const int DefaultLinesAmount = 8 * 1024;

    private readonly ILogger _logger;
    private readonly IComparer<ReadOnlyMemory<char>> _comparer;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(MaxDegreeOfParallelism);
    private readonly ConcurrentBag<string> _resultFiles = new ConcurrentBag<string>();

    public int BufferSize { get; init; }

    public LargeFileSeparatorSorter(ILogger logger, IComparer<ReadOnlyMemory<char>> comparer, int bufferSize)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        BufferSize = bufferSize < MinBufferSize ? MinBufferSize : bufferSize;
    }

    public async Task<string[]> SeparateToSortedFilesAsync(FileStream fileStream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(fileStream, detectEncodingFromByteOrderMarks: false, bufferSize: BufferSize);
        //TODO: ? buffered stream
        var tasks = new ConcurrentBag<Task>();
        while (true)
        {
            // _semaphore will be released inside sortAndSaveTask after the file will be written to disk
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Note: this violates Rule #3 from Memory<T> and Span<T> usage guidelines,
            // https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines
            // but I can't this of a better alternative 
            var bufferOwner = MemoryPool<char>.Shared.Rent(BufferSize);
            var buffer = bufferOwner.Memory;
            int bytesRead;
            if ((bytesRead = await reader.ReadAsync(buffer, cancellationToken)) == 0)
            {
                break;
            }

            var sliceRead = buffer[..bytesRead]; // bytesRead != buffer.Length

            var lastLineEnd = sliceRead.Span.LastIndexOf('\n');
            var activeSlice = lastLineEnd > 0 ? sliceRead[..lastLineEnd] : sliceRead;

            var sortAndSaveTask = Task.Factory.StartNew(
                async () =>
                {
                    try
                    {
                        await SortAndSaveBuffer(activeSlice, cancellationToken).ConfigureAwait(false);
                        bufferOwner.Dispose();
                        _semaphore.Release();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Exception in SortAndSaveBuffer: {Ex}", ex.Message);
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

            tasks.Add(sortAndSaveTask);

            // we go back where the last line started
            fileStream.Seek(-1 * (sliceRead.Length - lastLineEnd), SeekOrigin.Current);
        }

        await Task.WhenAll(tasks.ToArray());

        return _resultFiles.ToArray();
    }

    private async Task SortAndSaveBuffer(ReadOnlyMemory<char> activeBuffer, CancellationToken cancellationToken)
    {
        var resultLines = new List<ReadOnlyMemory<char>>(DefaultLinesAmount);
        int newLineLength = 0;
        while ((newLineLength = activeBuffer.Span.IndexOf('\n')) > 0)
        {
            var line = activeBuffer[..newLineLength].TrimEnd('\r');
            resultLines.Add(line);
            activeBuffer = activeBuffer[(newLineLength + 1)..];
        }

        resultLines.Sort(_comparer);

        var tempFilePath = Path.GetTempPath();
        await using var writer = new StreamWriter(tempFilePath, append: false, Encoding.ASCII, BufferSize);
        foreach (var line in resultLines)
        {
            await writer.WriteLineAsync(line, cancellationToken).ConfigureAwait(false);
        }

        _resultFiles.Add(tempFilePath);
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}