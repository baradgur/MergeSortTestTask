using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using AltiumTestTask.Sorter;

var fileToProcess = "testdata.txt";

var defaultBufferSize = 1024 * 1024 * 64; //64MB

var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5)); //TODO: to constants or remove
var cancellationToken = cancellationTokenSource.Token;

var smallTimeout = TimeSpan.FromMilliseconds(10);
//TODO: set bufferSize based on file size and available memory

await using FileStream fs = File.Open(fileToProcess, FileMode.Open, FileAccess.Read);

var availableMemory = GC.GetTotalMemory(false);

var fileLength = (ulong)fs.Length;

const ulong defaultMaxTempFiles = 256;
ulong bufferSizeEstimateByTempFileCount = fileLength / defaultMaxTempFiles + (ulong) (fileLength % defaultMaxTempFiles == 0 ? 0 : 1);
long bufferSizeEstimateByTempFileCountMaxPow2 = 1L << (BitOperations.Log2( bufferSizeEstimateByTempFileCount - 1) + 1);


int bufferSize = (int) (bufferSizeEstimateByTempFileCountMaxPow2 < defaultBufferSize
    ? defaultBufferSize
    : bufferSizeEstimateByTempFileCountMaxPow2);

bufferSize = (int) Math.Min(bufferSizeEstimateByTempFileCountMaxPow2, defaultBufferSize);
var maxBufferSizeByMemory = GetPowerOfTwoLessThanOrEqualTo(availableMemory/3);
bufferSize = Math.Max(bufferSize, maxBufferSizeByMemory);

int GetPowerOfTwoLessThanOrEqualTo(long x)
{
    return (x <= 0 ? 0 : (1 << (int)Math.Log(x, 2)));
}
                 
using var initiallySortedFiles = new BlockingCollection<string>();
long filesMerging = 0;
var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

var stopwatch = new Stopwatch();
stopwatch.Start();
var bulkTextReader = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded);


var comparer = new TextFormatDefaults.DataComparer();

var bagOfSortingTasks = new ConcurrentBag<Task>();

var bagOfMergingTasks = new BlockingCollection<Task>();
await foreach (var bulkOfLines in bulkTextReader
                   .ReadAllLinesBulkAsync(fs, cancellationToken)
                   .WithCancellation(cancellationToken))
{
    //TODO:  check all task finished successfully

    var task = Task.Factory.StartNew(
        () =>
        {
            semaphore.Wait();
            Array.Sort(bulkOfLines, comparer);
            var filename = Path.GetTempFileName();
            File.WriteAllLines(filename, bulkOfLines);
            initiallySortedFiles.Add(filename);
            semaphore.Release();
        },
        cancellationToken,
        TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
        TaskScheduler.Default);
    
    bagOfSortingTasks.Add(task);
}

await Task.WhenAll(bagOfSortingTasks);

initiallySortedFiles.CompleteAdding();
var initiallySortedFilesCopy = initiallySortedFiles.ToArray();

var filesToMerge = new ConcurrentQueue<string>();

for(int i = 0; i < initiallySortedFilesCopy.Length - 1; i += 2)
{
    if (i + 1 == initiallySortedFilesCopy.Length)
    {
        //odd number of files - we add one to the list to merge later
        filesToMerge.Enqueue(initiallySortedFilesCopy[i + 1]);
        break;
    }

    var i1 = i;
    var mergingTask =  Task.Factory.StartNew( 
         async () => await MergeFiles(initiallySortedFilesCopy[i1], initiallySortedFilesCopy[i1+1], cancellationToken).ConfigureAwait(false), 
         cancellationToken,
         TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
         TaskScheduler.Current);
     
     bagOfMergingTasks.Add(mergingTask);
}

foreach (var mergingTask in bagOfMergingTasks.GetConsumingEnumerable(cancellationToken))
{
    await mergingTask.ConfigureAwait(false);
    await Task.Delay(smallTimeout).ConfigureAwait(false);
}

stopwatch.Stop();

Console.WriteLine("Bye, world :"+ stopwatch.Elapsed);

async Task MergeFiles(
    string? firstFile,
    string? secondFile,
    CancellationToken cancellationToken1)
{
    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
    Interlocked.Increment(ref filesMerging);
    Console.WriteLine($"Started merging {firstFile} and {secondFile}");
    var stopwatchMerge = new Stopwatch();
    stopwatchMerge.Start();
    var fs1 = File.OpenRead(firstFile);
    var fs2 = File.OpenRead(secondFile);
    //TODO: use pool of readers?
    var bulkTextReader1 = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded);
    var bulkTextReader2 = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded);
    await using var enumerator1 = bulkTextReader1
        .ReadAllLinesAsync(fs1, cancellationToken1)
        .WithCancellation(cancellationToken1)
        .GetAsyncEnumerator();
    await using var enumerator2 = bulkTextReader2
        .ReadAllLinesAsync(fs2, cancellationToken1)
        .WithCancellation(cancellationToken1)
        .GetAsyncEnumerator();

    var fileName = Path.GetTempFileName();
    await using var writer = new StreamWriter(Path.GetTempFileName(), append: false, Encoding.ASCII, bufferSize);

    //TODO: set flushing by filesize
    var linesCount = 0;
    var estimatedLinesSize = 0;
    
    while (true)
    {
        var hasLines1 = await enumerator1.MoveNextAsync();
        var hasLines2 = await enumerator2.MoveNextAsync();
        if (!hasLines1)
        {
            while (await enumerator2.MoveNextAsync())
            {
                await writer.WriteAsync(enumerator2.Current);
                linesCount++;
            }

            break;
        }

        if (!hasLines2)
        {
            while (await enumerator1.MoveNextAsync())
            {
                await writer.WriteAsync(enumerator1.Current);
                linesCount++;
            }

            break;
        }

        if (comparer.Compare(enumerator1.Current, enumerator2.Current) <= 0)
        {
            await writer.WriteAsync(enumerator1.Current).ConfigureAwait(false);
            linesCount++;
        }
        else
        {
            await writer.WriteAsync(enumerator1.Current).ConfigureAwait(false);
            linesCount++;
        }

        if (linesCount == 10000)
        {
            await writer.FlushAsync().ConfigureAwait(false);
        }
    }

    await writer.FlushAsync().ConfigureAwait(false);
    writer.Close();
    fs1.Close();
    fs2.Close();
    File.Delete(firstFile);
    File.Delete(secondFile);
    stopwatch.Stop();
    Console.WriteLine($"Finished merging {firstFile} and {secondFile} in {stopwatch.Elapsed}");

    Interlocked.Decrement(ref filesMerging);
    
    var haveFileToMerge = filesToMerge.TryDequeue(out var nextFileName);
    var filesCurrentlyMerging = Interlocked.Read(ref filesMerging);
    if (!haveFileToMerge && filesCurrentlyMerging == 0)
    {
        //this is a last file, all done
        Console.WriteLine($"Last file, all done");
        bagOfMergingTasks.CompleteAdding();
        File.Move(fileName, "sorted.txt", true);
        semaphore.Release();
        return;
    }

    if (haveFileToMerge)
    {
        var newMergingTask = Task.Factory.StartNew(
            async () => await MergeFiles(fileName, nextFileName, cancellationToken).ConfigureAwait(false),
            cancellationToken,
            TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Current);
        bagOfMergingTasks.Add(newMergingTask);
    }

    if (!haveFileToMerge && filesCurrentlyMerging > 0)
    {
        filesToMerge.Enqueue(fileName);
    }
    
    semaphore.Release();
    Console.WriteLine($"Finished merging {firstFile} and {secondFile} - released semaphore");
}
