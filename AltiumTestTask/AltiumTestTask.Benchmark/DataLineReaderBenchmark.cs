using AltiumTestTask.Sorter;
using AltiumTestTask.TestFileGenerator;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace AltiumTestTask.Benchmark;

[SimpleJob(RuntimeMoniker.Net70)]
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
    public void DataLineReader()
    {
        var reader = new DataLineWithSeparatorReader();
        var array = reader.GetData().ToArray();
    }
    
    [Benchmark]
    public void DataLineReaderMod()
    {
        var reader = new DataLineWithSeparatorReader();
        var array = reader.GetDataMod().ToArray();
    }
    
    [Benchmark]
    public void FileReadAllLines()
    {
        File.ReadAllLines(Defaults.TestFilename);
    }
}