using System.Diagnostics;
using System.Text;

namespace AltiumTestTask.Sorter;

public class DataLineWithSeparatorReader
{
    //TODO: optimize buffer size (make less than LOH?)
    const int MAX_BUFFER = 33554432; //32MB
    //const int MAX_BUFFER = 1024*4; //32MB
    byte[] buffer = new byte[MAX_BUFFER];
    //TODO: increase this buffer size?
    char[] smallbuffer = new char[1];
    //some allocation tradeoff - may be optimized
    const int _maxStringSize = 512;
    //TODO: make thread-safe? or not needed?
    StringBuilder currentLineBuilder = new StringBuilder(_maxStringSize);
    private int currentPositionInString = 0;
    private int currentSeparatorPositionString = -1;

    public IEnumerable<DataLineWithSeparator> GetData()
    {
        using FileStream fs = File.Open("testdata.txt", FileMode.Open, FileAccess.Read);
        using BufferedStream bufferedStream = new BufferedStream(fs);
    
        var lineCount = 0;
        int bytesRead = 0;
        using var memoryStream = new MemoryStream(buffer);
        using var streamReader = new StreamReader(memoryStream);
        while ((bytesRead = bufferedStream.Read(buffer, 0, MAX_BUFFER)) != 0)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            while (!streamReader.EndOfStream && bytesRead > 0)
            {
                var data = ReadLineWithAccumulation(streamReader, ref bytesRead);
                
                if (data == null || string.IsNullOrEmpty(data.Data))
                {
                    //Console.WriteLine($"lineCount: '{lineCount}', reading next buffer");
                    break;
                }
#if DEBUG
                // Debug.Assert(char.IsDigit(data.Data[0]) || data.Data[0] == '-', "wrong line separation!");
                // Debug.Assert(data.separatorPosition != 0,"no separator in line!");
                // Debug.Assert(data.Data[data.separatorPosition] == '.',"wrong separation detection");
#endif
                lineCount++;
                //Console.WriteLine($"line {lineCount} data: '{data.Data}' separator: '{data.separatorPosition}'");
                yield return data;
            }
            //TODO: think: return array of lines here?
        }
    }
    
    private DataLineWithSeparator? ReadLineWithAccumulation(StreamReader stream, ref int bytesleft)
    {
        //while (bytesLeft > 0 && stream.Read(smallbuffer, 0, 1) > 0)
        while (stream.Read(smallbuffer, 0, 1) > 0)
        {
            bytesleft--;
            //TODO: to const or settings
            if (smallbuffer[0].Equals('.') && currentSeparatorPositionString == -1)
            {
                //is there are several dots the first one will be used
                currentSeparatorPositionString = currentPositionInString;
            }

            currentPositionInString++;
            //TODO: to const or settings
            if (smallbuffer[0].Equals('\n') || bytesleft < 0)
            {
                //NOTE: string allocation is happening here!
                var result = currentLineBuilder.ToString();
                currentLineBuilder.Clear();
                currentPositionInString = 0;
                var currentSeparatorPositionStringResult = currentSeparatorPositionString;
                currentSeparatorPositionString = -1;
                return new DataLineWithSeparator(result, currentSeparatorPositionStringResult);
            }
            else
            {
                currentLineBuilder.Append(smallbuffer[0]);
            }
        }
        Console.WriteLine("line not complete yet");
        return null; //line not complete yet
    }
}