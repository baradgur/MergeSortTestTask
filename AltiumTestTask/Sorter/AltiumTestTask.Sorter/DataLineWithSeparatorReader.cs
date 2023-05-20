using System.Diagnostics;
using System.Text;

namespace AltiumTestTask.Sorter;

public class DataLineWithSeparatorReader
{
    //TODO: optimize buffer size (make less than LOH?)
    //const int MAX_BUFFER = 33554432; //32MB
    const int MAX_BUFFER = 32*1024*1024; //32MB
    byte[] buffer = new byte[MAX_BUFFER];
    //TODO: increase this buffer size?
    char[] smallbuffer = new char[1];
    //some allocation tradeoff - may be optimized
    const int _maxStringSize = 512;
    const int _medianStringSize = 128;
    //TODO: make thread-safe? or not needed?
    //StringBuilder currentLineBuilder = new StringBuilder(_medianStringSize, _maxStringSize);
    StringBuilder currentLineBuilder = new StringBuilder(_maxStringSize);
    private int currentPositionInString = 0;
    private int currentSeparatorPositionString = -1;

    public IEnumerable<DataLineWithSeparator> GetData()
    {
        using FileStream fs = File.Open("testdata.txt", FileMode.Open, FileAccess.Read);
        //using BufferedStream bufferedStream = new BufferedStream(fs);
    
        var lineCount = 0;
        int bytesRead = 0;
        using var memoryStream = new MemoryStream(buffer, false);
        using var streamReader = new StreamReader(memoryStream, detectEncodingFromByteOrderMarks: false);
        while ((bytesRead = fs.Read(buffer, 0, MAX_BUFFER)) != 0)
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
        while (stream.Read(smallbuffer, 0, 1) > 0)
        //while (stream.ReadLine() > 0)
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
    
    
    public IEnumerable<DataLineWithSeparator> GetDataMod()
    {
        using FileStream fs = File.Open("testdata.txt", FileMode.Open, FileAccess.Read);
        using BufferedStream bufferedStream = new BufferedStream(fs, MAX_BUFFER);
    
        var lineCount = 0;
        int bytesRead = 0;
        var bufferReads = 0; 
        using var memoryStream = new MemoryStream(buffer, false);
        using var streamReader = new StreamReader(memoryStream, detectEncodingFromByteOrderMarks: false);
        while ((bytesRead = bufferedStream.Read(buffer, 0, MAX_BUFFER)) != 0)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            bufferReads++;
            var firstReadInBuffer = true;
            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }
                lineCount++;
                //checking if the line is the last one in buffer
                if (streamReader.EndOfStream) 
                {
                    //save last line of the big buffer - it may be appended to when reading the next buffer
                    currentLineBuilder.Append(line);
                    break;
                }
                
                if (firstReadInBuffer)
                {
                    firstReadInBuffer = false;
                    if (currentLineBuilder.Length > 0)
                    {
                        var oldLine = currentLineBuilder.ToString();
                        currentLineBuilder.Clear();
                        // if old line or new line is not in a correct format, we merge them
                        if (
                            !oldLine.Contains('.')
                            ||
                            //new line is not in a correct format
                            !line.Contains('.') || !(line[0] == '-' || char.IsDigit(line[0]))
                            )
                        {
                            var result = oldLine + line;
                            yield return new DataLineWithSeparator(result, result.IndexOf('.'));
                            continue;
                        }
                        // if both lines are correct - we return both of them
                        yield return new DataLineWithSeparator(oldLine, oldLine.IndexOf('.'));
                        yield return new DataLineWithSeparator(line, line.IndexOf('.'));
                        continue;
                    }
                }
#if DEBUG
                // Debug.Assert(char.IsDigit(line[0]) || line[0] == '-', "wrong line separation!");
                // Debug.Assert(line.Contains('.'), "no separator in line!");
                // Debug.Assert(line.IndexOf('.') == line.LastIndexOf('.'), "wrong separation detection");
#endif
                
                //Console.WriteLine($"line {lineCount} data: '{data.Data}' separator: '{data.separatorPosition}'");
                yield return new DataLineWithSeparator(line, line.IndexOf('.'));
            }
        }
        //last string will nothing to append to it, so we return it as is
        if (bytesRead == 0)
        {
            var line = currentLineBuilder.ToString();
            yield return new DataLineWithSeparator(line, line.IndexOf('.'));
        }
    }
}