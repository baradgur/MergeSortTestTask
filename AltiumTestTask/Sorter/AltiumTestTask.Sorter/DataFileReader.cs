using System.Diagnostics;
using System.Text;

namespace AltiumTestTask.Sorter;

public class DataLineReader
{
    //TODO: optimize buffer size (make less than LOH?)
    const int MAX_BUFFER = 33554432; //32MB
    byte[] buffer = new byte[MAX_BUFFER];
    //TODO: increase this buffer size?
    char[] smallbuffer = new char[1];
    //some allocation tradeoff - may be optimized
    const int _maxStringSize = 512;
    StringBuilder currentLineBuilder = new StringBuilder(_maxStringSize);

    IEnumerable<string> GetStrings()
    {
        using FileStream fs = File.Open("testdata.txt", FileMode.Open, FileAccess.Read);
        using BufferedStream bufferedStream = new BufferedStream(fs);
    
        var lineCount = 0;
        using var memoryStream = new MemoryStream(buffer);
        using var streamReader = new StreamReader(memoryStream);
        while (( bufferedStream.Read(buffer, 0, MAX_BUFFER)) != 0)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
    
            while (!streamReader.EndOfStream)
            {
                var line = ReadLineWithAccumulation(streamReader);
                lineCount++;
                if (string.IsNullOrEmpty(line))
                {
                    Console.WriteLine($"lineCount: '{lineCount}'");
                    break;
                }
#if DEBUG
                Debug.Assert(!(char.IsDigit(line[0]) || line[0] == '-'), "wrong line separation!");
#endif
                yield return line;
            }
            //TODO: return array of lines here?
        }
    }
    
    private string? ReadLineWithAccumulation(StreamReader stream)
    {
        while (stream.Read(smallbuffer, 0, 1) > 0)
        {
            if (smallbuffer[0].Equals('\n'))
            {
                //NOTE: string allocation is happening here!
                var result = currentLineBuilder.ToString();
                currentLineBuilder.Clear();
    
                return result;
            }
            else
            {
                currentLineBuilder.Append(smallbuffer[0]);
            }
        }
    
        return null; //line not complete yet
    }
}