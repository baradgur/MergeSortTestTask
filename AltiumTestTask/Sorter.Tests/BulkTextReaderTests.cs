using System.Text;

namespace AltiumTestTask.Sorter.Tests;

public class BulkTextReaderTests
{
    private readonly string _sampleString1 = "123. Apple is bad";
    private readonly string _sampleString2 = "456. Banana is good";
    private readonly string _sampleString3 = "789. Tangerine is somewhat okay if you need vitamins, but too hard to peel";

    [Theory]
    [InlineData("123. Apple is bad")]
    [InlineData("123. Apple is bad\n")]
    [InlineData("123. Apple is bad\r\n")]
    [InlineData("123. Apple is bad\n456. Banana is good")]
    [InlineData("123. Apple is bad\r\n456. Banana is good")]
    [InlineData("123. Apple is bad\n456. Banana is good\n")]
    [InlineData("123. Apple is bad\r\n456. Banana is good\n")]
    [InlineData("123. Apple is bad\n456. Banana is good\r\n")]
    [InlineData("123. Apple is bad\n456. Banana is good\n789. Tangerine is somewhat okay")]
    public void StreamIsTooSmall(string data)
    {
        var textBuffer = Encoding.ASCII.GetBytes(data);
        var memoryStream = new MemoryStream(textBuffer);
        var reader = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded, BulkTextReader.MinBufferSize);
        var result = reader.ReadAllLinesBulk(memoryStream).SelectMany(a => a).ToArray();

        var expected = data.Split('\n').Select(s => s.TrimEnd('\r')).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        Assert.Equal(expected.Length, result.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], result[i]);
        }
    }

    [Fact]
    public void BulksAreSeparatedPerfectlyByBufferEdge()
    {
        var bufferSize = 128;
        var startContent = _sampleString1 + "\n" + _sampleString2;
        var bulk1 = startContent + new string('a', bufferSize - startContent.Length);
        Assert.Equal(bufferSize, bulk1.Length);

        string bulk2 = _sampleString3 + new string('b', bufferSize - _sampleString3.Length - 1) + "\n";
        Assert.Equal(bulk2.Length, bufferSize);

        var textBuffer = Encoding.ASCII.GetBytes(bulk1 + bulk2);
        Assert.Equal(bufferSize * 2, textBuffer.Length);
        var memoryStream = new MemoryStream(textBuffer);
        var reader = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded, bufferSize);
        var result = reader.ReadAllLinesBulk(memoryStream).SelectMany(a => a).ToArray();
        Assert.True(3 == result.Length, $"Actual result: {string.Join(',', result)}");
        Assert.Equal(_sampleString1, result[0]);
        Assert.StartsWith(_sampleString2, result[1]);
        Assert.StartsWith(_sampleString3, result[2]);
    }

    [Fact]
    public void TwoBulksContainOnlyByOneLineEach()
    {
        var bufferSize = 128;
        var bulk1 = _sampleString1 + new string('a', bufferSize - _sampleString1.Length - 1) + "\n";
        Assert.Equal(bufferSize, bulk1.Length);

        string bulk2 = _sampleString2 + new string('a', bufferSize - _sampleString2.Length);
        Assert.Equal(bulk2.Length, bufferSize);

        var textBuffer = Encoding.ASCII.GetBytes(bulk1 + bulk2);

        Assert.Equal(bufferSize * 2, textBuffer.Length);

        var memoryStream = new MemoryStream(textBuffer);
        var reader = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded, bufferSize);
        var result = reader.ReadAllLinesBulk(memoryStream).SelectMany(a => a).ToArray();

        Assert.True(2 == result.Length, $"Actual result: {string.Join(',', result)}");
        Assert.StartsWith(_sampleString1, result[0]);
        Assert.StartsWith(_sampleString2, result[1]);
    }

    [Fact]
    public void LinesAreSeparatedByBufferEdge_BeforeDot()
    {
        var bufferSize = 128;
        var linePartInBulk1 = "45"; //in bulk1
        var linePartInBulk2 = "6. Banana is good"; //in bulk2

        var bulk1 = _sampleString1 + new string('a', bufferSize - _sampleString1.Length - 1 - linePartInBulk1.Length) 
                                   + "\n" + linePartInBulk1;
        Assert.Equal(bufferSize, bulk1.Length);

        string bulk2 = linePartInBulk2 + new string('a', bufferSize - linePartInBulk2.Length);
        Assert.Equal(bulk2.Length, bufferSize);

        var textBuffer = Encoding.ASCII.GetBytes(bulk1 + bulk2);

        Assert.Equal(bufferSize * 2, textBuffer.Length);

        var memoryStream = new MemoryStream(textBuffer);
        var reader = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded, bufferSize);
        var result = reader.ReadAllLinesBulk(memoryStream).SelectMany(a => a).ToArray();

        Assert.True(2 == result.Length, $"Actual result: {string.Join(',', result)}");
        Assert.StartsWith(_sampleString1, result[0]);
        Assert.StartsWith(linePartInBulk1 + linePartInBulk2, result[1]);
    }

    [Fact]
    public void LinesAreSeparatedByBufferEdge_AfterDot()
    {
        var bufferSize = 128;
        var linePartInBulk1 = "456. B"; //in bulk1
        var linePartInBulk2 = "anana is good"; //in bulk2

        var bulk1 = _sampleString1 + new string('a', bufferSize - _sampleString1.Length - 1 - linePartInBulk1.Length) 
                                   + "\n" + linePartInBulk1;
        Assert.Equal(bufferSize, bulk1.Length);

        string bulk2 = linePartInBulk2 + new string('a', bufferSize - linePartInBulk2.Length);
        Assert.Equal(bulk2.Length, bufferSize);

        var textBuffer = Encoding.ASCII.GetBytes(bulk1 + bulk2);

        Assert.Equal(bufferSize * 2, textBuffer.Length);

        var memoryStream = new MemoryStream(textBuffer);
        var reader = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded, bufferSize);
        var result = reader.ReadAllLinesBulk(memoryStream).SelectMany(a => a).ToArray();

        Assert.True(2 == result.Length, $"Actual result: {string.Join(',', result)}");
        Assert.StartsWith(_sampleString1, result[0]);
        Assert.StartsWith(linePartInBulk1 + linePartInBulk2, result[1]);
    }
    
    [Fact]
    public void LastBulkIsSmaller()
    {
        var bufferSize = 128;
        var bulk1 = _sampleString1 + new string('a', bufferSize - _sampleString1.Length - 1) + "\n";
        Assert.Equal(bufferSize, bulk1.Length);

        string bulk2 = _sampleString2;

        var textBuffer = Encoding.ASCII.GetBytes(bulk1 + bulk2);

        var memoryStream = new MemoryStream(textBuffer);
        var reader = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded, bufferSize);
        var result = reader.ReadAllLinesBulk(memoryStream).SelectMany(a => a).ToArray();

        Assert.True(2 == result.Length, $"Actual result: {string.Join(',', result)}");
        Assert.StartsWith(_sampleString1, result[0]);
        Assert.StartsWith(_sampleString2, result[1]);
    }
    
    [Fact]
    public void LastBulkIsSmaller_AndEndsWithLineTerminator()
    {
        var bufferSize = 128;
        var bulk1 = _sampleString1 + new string('a', bufferSize - _sampleString1.Length - 1) + "\n";
        Assert.Equal(bufferSize, bulk1.Length);

        string bulk2 = _sampleString2 + "\n";

        var textBuffer = Encoding.ASCII.GetBytes(bulk1 + bulk2);

        var memoryStream = new MemoryStream(textBuffer);
        var reader = new BulkTextReader(TextFormatDefaults.IsConcatenationNeeded, bufferSize);
        var result = reader.ReadAllLinesBulk(memoryStream).SelectMany(a => a).ToArray();

        Assert.Equal(2, result.Length);
        Assert.StartsWith(_sampleString1, result[0]);
        Assert.StartsWith(_sampleString2, result[1]);
    }
}