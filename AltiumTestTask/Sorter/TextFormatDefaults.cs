namespace AltiumTestTask.Sorter;

public static class TextFormatDefaults
{
    public static bool IsConcatenationNeeded(string oldLine, string line)
    {
        return !oldLine.Contains('.')
               ||
               //new line is not in a correct format
               !line.Contains('.') || !(line[0] == '-' || char.IsDigit(line[0]));
    }
}