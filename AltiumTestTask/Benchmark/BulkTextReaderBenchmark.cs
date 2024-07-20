using System.Globalization;
using MergeSortTestTask.Sorter;
using MergeSortTestTask.TestFileGenerator;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Serilog.Core;

namespace MergeSortTestTask.Benchmark;

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
[RPlotExporter]
public class BulkTextReaderBenchmark
{
    [Benchmark(Baseline = true)]
    public void FileReadAllLines()
    {
        File.ReadAllLines(Defaults.TestFilename);
    }

    [Benchmark]
    public async Task BulkTextReader()
    {
        var reader = new BulkTextReader(Logger.None, TextFormatDefaults.IsConcatenationNeeded);
        await using var fs = File.OpenRead(Defaults.TestFilename);
        await foreach (var bulk in reader.ReadAllLinesBulkAsync(fs, CancellationToken.None))
        {
        }
    }

    [GlobalSetup]
    public async Task Setup()
    {
        var fileTestFileCreator = new TestFileCreator();
        await fileTestFileCreator.CreateFile(
            new FileInfo(Defaults.TestFilename),
            new SizeCalculationOptions(SizeCalculationMethod.MegaBytes, 100));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        File.Delete(Defaults.TestFilename);
    }
}