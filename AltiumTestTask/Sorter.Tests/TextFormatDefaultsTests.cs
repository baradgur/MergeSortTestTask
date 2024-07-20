using System.Text;
using MergeSortTestTask.Sorter;

namespace MergeSortTestTask.Sorter.Tests;

public class TextFormatDefaultsTests
{
    [Theory]
    [InlineData("123. Apple is bad", "123. Apple is bad", 0)]
    [InlineData("987. Apple is bad", "123. Banana is good", -1)]
    [InlineData("987. Apple is bad", "123. Apple is bad", 1)]
    [InlineData("123. Apple is bad", "987. Apple is bad", -1)]
    [InlineData("123. Apple is bad", "1234. Apple is bad", -1)]
    [InlineData("123. Apple is bad", "-12. Apple is bad", 1)]
    public void DataComparer_Compare(string x, string y, int result)
    {
        var comparer = new TextFormatDefaults.DataComparer();
        Assert.Equal(result, comparer.Compare(x, y));
        if (result != 0)
        {
            Assert.Equal(-result, comparer.Compare(y, x));
        }
    }

    [Fact]
    public void DataComparer_CompareSameString()
    {
        var comparer = new TextFormatDefaults.DataComparer();
        var testString = "testString";
        Assert.Equal(0, comparer.Compare(testString, testString));
    }

    [Fact]
    public void DataComparer_Sort()
    {
        var array = new[]
        {
            "123. Apple is bad",
            "\n",
            "123. Apple is bad",
            "-1. Apple is bad",
            "456. Banana is good",
            "789. Tangerine is somewhat okay if you need vitamins, but too hard to peel",
        };
        var comparer = new TextFormatDefaults.DataComparer();
        Array.Sort(array, comparer);
    }
}