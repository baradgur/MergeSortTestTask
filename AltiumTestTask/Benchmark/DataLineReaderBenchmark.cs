using AltiumTestTask.Sorter;
using AltiumTestTask.TestFileGenerator;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace AltiumTestTask.Benchmark;

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
[RPlotExporter]
public class DataLineReaderBenchmark
{
    [GlobalSetup]
    public async Task Setup()
    {
        var fileTestFileCreator = new TestFileCreator();
        await fileTestFileCreator.CreateFile(
            new FileInfo(Defaults.TestFilename),
            new SizeCalculationOptions(SizeCalculationMethod.MegaBytes, 100));
    }
    
    [Benchmark]
    public void BulkTextReader()
    {
        var reader = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded);
        using var fs = File.OpenRead(Defaults.TestFilename);
        foreach (var bulk in reader.ReadAllLinesBulk(fs))
        {
        }
    }
    
    [Benchmark]
    public void BulkTextReader_WithBuffering()
    {
        var reader = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded);
        using var fs = File.OpenRead(Defaults.TestFilename);
        foreach (var bulk in reader.ReadAllLinesBulkWithBuffering(fs))
        {
        }
    }
    
    // [Benchmark]
    // public void FileReadAllLines()
    // {
    //     File.ReadAllLines(Defaults.TestFilename);
    // }
}