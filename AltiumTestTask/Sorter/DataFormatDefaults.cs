using System.Runtime.CompilerServices;

namespace AltiumTestTask.Sorter;

public static class DataFormatDefaults
{
    public static char StringNumberSeparationChar = '.';
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConcatenationNeeded(string oldLine, string line)
    {
        return
            //old line is not in a correct format
            !oldLine.Contains(StringNumberSeparationChar)
            ||
            //new line is not in a correct format
            !line.Contains(StringNumberSeparationChar) || !(line[0] == '-' || char.IsDigit(line[0]));
    }

    public class Comparer : IComparer<string>, IComparer<ReadOnlyMemory<char>>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(string? x, string? y)
        {
            return Compare(x.AsSpan(), y.AsSpan());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        {
            return Compare(x.Span, y.Span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
        {
            var dot1 = x.IndexOf(StringNumberSeparationChar);
            var dot2 = y.IndexOf(StringNumberSeparationChar);
            if (dot1 == -1 || dot2 == -1)
            {
                return x.CompareTo(y, StringComparison.InvariantCulture);
            }
            var stringComparison = x[dot1..].CompareTo(y[dot2..], StringComparison.InvariantCulture);
            return stringComparison != 0
                ? stringComparison
                : long.Parse(x[..dot1]).CompareTo(long.Parse(y[..dot2]));
        }
    }
}