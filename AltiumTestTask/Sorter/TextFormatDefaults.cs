using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AltiumTestTask.Sorter;

public static class TextFormatDefaults
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

    public class DataComparer : IComparer<string>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(string? x, string? y)
        {
            Debug.Assert(x != null, "file format is incorrect");
            Debug.Assert(y != null, "file format is incorrect");
            var span1 = x.AsSpan();
            var dot1 = x.IndexOf('.');
            var span2 = y.AsSpan();
            var dot2 = y.IndexOf('.');
            Debug.Assert(dot1 != -1, "file format is incorrect");
            Debug.Assert(dot2 != -1, "file format is incorrect");
            if (dot1 == -1 || dot2 == -1)
            {
                return span1.CompareTo(span2, StringComparison.InvariantCulture);
            }
            
            var stringComparison = span1[dot1..].CompareTo(span2[dot2..], StringComparison.InvariantCulture);
            return stringComparison != 0
                ? stringComparison
                : long.Parse(span1[..dot1]).CompareTo(long.Parse(span2[..dot2]));
        }
    }
}