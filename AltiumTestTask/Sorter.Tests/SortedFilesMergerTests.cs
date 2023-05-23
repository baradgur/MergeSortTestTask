using System.Linq;
using Serilog.Core;

namespace AltiumTestTask.Sorter.Tests;

public class SortedFilesMergerTests
{
    [Theory]
    [InlineData("123. Apple is bad\n456. Banana is good", "23. Apple is bad\n56. Banana is good")]
    public async Task MergeSmallFiles(string data1, string data2)
    {
        var dataArray1 = data1.Split('\n');
        var dataArray2 = data2.Split('\n');
        
        var file1 = Path.GetTempFileName();
        var file2 = Path.GetTempFileName();
        var files = new[] { file1, file2 };
        await File.WriteAllTextAsync(file1, data1);
        await File.WriteAllTextAsync(file2, data2);
        var comparer = new TextFormatDefaults.DataComparer();
        var bulkTextReaderPool = new ObjectPool<IBulkTextReader>(
            () => new BulkTextReader(Logger.None, TextFormatDefaults.IsConcatenationNeeded, 128));
        var merger = new SortedFilesFilesMerger(
            Logger.None,
            128,
            comparer,
            bulkTextReaderPool);
        var resultFile = await merger.MergeFilesAsync(files, CancellationToken.None);
        var result = await File.ReadAllLinesAsync(resultFile, CancellationToken.None);
        Assert.Equal(dataArray1.Length + dataArray2.Length, result.Length);
        var y = result.First();
        var isSorted = result
            .Skip(1)
            .All(x =>
            {
                var b = comparer.Compare(y, x) < 0;
                y = x;
                return b;
            });
        Assert.True(isSorted);
        File.Delete(file1);
        File.Delete(file2);
        File.Delete(resultFile);
    }
}

public class MergerHelperTests
{
    [Theory]
    [InlineData("123. Apple is bad\n456. Banana is good", "23. Apple is bad\n56. Banana is good")]
    [InlineData("23. Apple is bad\n56. Banana is good", "123. Apple is bad\n456. Banana is good")]
    [InlineData("56. Banana is good", "123. Apple is bad\n456. Banana is good")]
    [InlineData("23. Apple is bad\n56. Banana is good", "123. Apple is bad")]
    public async Task MergeSmallArrays(string data1, string data2)
    {
        var dataArray1 = data1.Split('\n');
        var sequence1 = dataArray1.ToAsyncEnumerable();
        var dataArray2 = data2.Split('\n');
        var sequence2 = dataArray2.ToAsyncEnumerable();

        var comparer = new TextFormatDefaults.DataComparer();
        
        var result = await MergerHelper
            .MergePreserveOrderAsync(sequence1, sequence2, comparer, CancellationToken.None)
            .ToArrayAsync();
        
        Assert.Equal(dataArray1.Length + dataArray2.Length, result.Length);
        var y = result.First();
        var isSorted = result
            .Skip(1)
            .All(x =>
            {
                var b = comparer.Compare(y, x) < 0;
                y = x;
                return b;
            });
        Assert.True(isSorted);
    }
}