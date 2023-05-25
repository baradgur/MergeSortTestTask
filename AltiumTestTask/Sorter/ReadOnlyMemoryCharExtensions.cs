namespace AltiumTestTask.Sorter;

public static class ReadOnlyMemoryCharExtensions
{
    public static IEnumerable<ReadOnlyMemory<char>> SeparateToLines(this ReadOnlyMemory<char> buffer)
    {
        int newLineLength = 0;
        var activeBuffer = buffer[..];
        while ((newLineLength = activeBuffer.Span.IndexOf('\n')) > 0)
        {
            var line = activeBuffer[..newLineLength].TrimEnd('\r');
            yield return line;
            activeBuffer = activeBuffer[(newLineLength + 1)..];
        }
    } 
}