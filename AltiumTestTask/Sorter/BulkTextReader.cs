using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace AltiumTestTask.Sorter;

public class BulkTextReader : IBulkTextReader
{
    public delegate bool IsConcatenationNeededCheck(string oldLine, string newLine);

    public const int MinBufferSize = 128; //equals StreamReader.MinBufferSize
    public const int DefaultMaxBuffer = 32*1024*1024; //32MB
    public const int DefaultLineSize = 128; //set empirically, based on StreamReader code

    public int BufferSize { get; }
    
    private readonly IsConcatenationNeededCheck _isConcatenationNeeded;
    private readonly byte[] _buffer;
    private readonly StringBuilder _lineBuilder = new StringBuilder(DefaultLineSize);
    
    public BulkTextReader(IsConcatenationNeededCheck isConcatenationNeeded, int bufferSize = DefaultMaxBuffer)
    {
        if (bufferSize < MinBufferSize)
        {
            bufferSize = MinBufferSize;
        }
        BufferSize = bufferSize;
        _buffer = new byte[bufferSize];
        _isConcatenationNeeded = isConcatenationNeeded;
    }

    public IEnumerable<ImmutableArray<string>> ReadAllLinesBulk(Stream stream)
    {
        int bytesRead;
        if (stream.Length < BufferSize)
        {
            //Debug.Assert(false, $"Do not use {nameof(BulkTextReader)} with small streams. It's inefficient.");
            using TextReader tr = new StreamReader(stream);
            var strings = tr.ReadToEnd();
            yield return strings
                .Split('\n')
                .Select(s => s.TrimEnd('\r'))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToImmutableArray();
            yield break;
        }

        //buffer size was set after some tests
        var bufferedStream = new BufferedStream(stream, BufferSize);
        using var memoryStream = new MemoryStream(_buffer, false);
        using var streamReader = new StreamReader(memoryStream);
        while ((bytesRead = bufferedStream.Read(_buffer, 0, BufferSize)) != 0)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            if (bytesRead < BufferSize)
            {
                //on a last read the buffer may still contain data from the previous read and it need to be cleared.
                Array.Clear(_buffer, bytesRead, _buffer.Length - bytesRead);
            }
            var lines = new List<string>();
            var firstReadInBuffer = true;
            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }
                //checking if the line is the last one in buffer (but not the only one)
                if (streamReader.EndOfStream && !firstReadInBuffer) 
                {
                    //save last line of the buffer - it may be appended to when reading the next buffer
                    if (line.StartsWith('\0'))
                    {
                        break;//HACK: for when readers returns rest of the buffer (filled with '\0')
                              //if the last character in stream was '\n'
                    }
                    _lineBuilder.Append(line);
                    break;
                }
                
                if (firstReadInBuffer)
                {
                    firstReadInBuffer = false;
                    if (_lineBuilder.Length > 0)
                    {
                        var oldLine = _lineBuilder.ToString();
                        _lineBuilder.Clear();
                        // if old line or new line is not in a correct format, we merge them
                        if (_isConcatenationNeeded(oldLine, line))
                        {
                            var result = oldLine + line;
                            //yield return new DataLineWithSeparator(result, result.IndexOf('.'));
                            lines.Add(result);
                            continue;
                        }
                        // if both lines are correct - we return both of them
                        lines.Add(oldLine);
                        lines.Add(line);
                        continue;
                    }
                }
                lines.Add(line);
            }
            yield return lines.ToImmutableArray();
        }
        //last string will nothing to append to it, so we return it as is
        if (bytesRead == 0 && _lineBuilder.Length > 0)
        {
            var line = _lineBuilder.ToString();
            _lineBuilder.Clear();
            yield return ImmutableArray.Create(line);
        }
    }
}