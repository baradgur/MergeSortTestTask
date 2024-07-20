using System.Diagnostics;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.FastConsole;

namespace AltiumTestTask.Sorter;

internal static class Program
{
    private static readonly TimeSpan ProgramFailedTimeout = TimeSpan.FromMinutes(10);

    //TODO: parse args
    //TODO: filenames
    public static async Task Main(string[] args)
    {
#if DEBUG
        var options = new FastConsoleSinkOptions() { UseJson = false };
        Log.Logger = new LoggerConfiguration().WriteTo.FastConsole(options).MinimumLevel.Verbose().CreateLogger();
#else
        Log.Logger = Logger.None;
        // var options = new FastConsoleSinkOptions() { UseJson = false };
        // Log.Logger = new LoggerConfiguration().WriteTo.FastConsole(options).MinimumLevel.Verbose().CreateLogger();
#endif
        var fileToProcess = "testdata.txt";

        var cancellationTokenSource = new CancellationTokenSource(ProgramFailedTimeout);
        var cancellationToken = cancellationTokenSource.Token;

        //var bufferSize = BufferSizeHelper.EstimateBufferSize();
        var bufferSize = BufferSizeHelper.MinBufferSize * 2;
#warning experimenting with buffersize

        Log.Logger.Information(
            "Estimated buffer size is: '{BufferSize}' that in MB is {BufferSizeInMb}",
            bufferSize,
            bufferSize / 1024 / 1024);
        Console.WriteLine($"Estimated buffer size is: '{bufferSize}' that in MB is {bufferSize / 1024 / 1024}");

        var comparer = new TextFormatDefaults.DataComparer();

        await using FileStream fs = File.Open(fileToProcess, FileMode.Open, FileAccess.Read);

        var bulkTextReaderPool = new BulkReaderPool(
            () =>
            {
                Log.Logger.Verbose("Created new BulkTextReader");
                return new BulkTextReader(Log.Logger, TextFormatDefaults.IsConcatenationNeeded, bufferSize);
            });

        using var separator = new Separator(bulkTextReaderPool, comparer);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var initiallySortedFiles = await separator.SeparateAndSortAsync(fs, cancellationToken);

        Console.WriteLine($"Finished separating in: {stopwatch.Elapsed}");
        if (initiallySortedFiles.Length == 1)
        {
            File.Move(initiallySortedFiles[0], "sorted.txt", true);
            stopwatch.Stop();
            Log.Logger.Information("Sorted in: {StopwatchElapsed}", stopwatch.Elapsed);
            await Log.CloseAndFlushAsync();
            return;
        }

        var merger = new SortedFilesFilesMerger(Log.Logger, bufferSize, comparer, bulkTextReaderPool);
        var pathToResultTempFile = await merger.MergeFilesAsync(initiallySortedFiles, cancellationToken);
        File.Move(pathToResultTempFile, "sorted.txt", true);

        stopwatch.Stop();

        Log.Logger.Information("Sorted in: {StopwatchElapsed}", stopwatch.Elapsed);
        Console.WriteLine($"Sorted in: {stopwatch.Elapsed}");
        await Log.CloseAndFlushAsync();
    }
}