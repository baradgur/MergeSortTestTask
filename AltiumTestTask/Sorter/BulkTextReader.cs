using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Serilog;

namespace AltiumTestTask.Sorter;

public class BulkTextReader : IBulkTextReader
{
    private readonly ILogger _logger;
    public const int MinBufferSize = 128; //equals StreamReader.MinBufferSize
    public const int DefaultMaxBuffer = 32 * 1024 * 1024; //32MB
    public const int DefaultLineSize = 128; //set empirically, based on StreamReader code
    public const int InitialListCapacity = 1024 * 32;
    public readonly List<string> Lines = new List<string>(InitialListCapacity);
    
    public int BufferSize { get; }

    private readonly IsConcatenationNeededCheck _isConcatenationNeeded;
    private readonly byte[] _buffer;
    private readonly StringBuilder _lineBuilder = new StringBuilder(DefaultLineSize);


    public BulkTextReader(
        ILogger logger,
        IsConcatenationNeededCheck isConcatenationNeeded,
        int bufferSize = DefaultMaxBuffer)
    {
        if (bufferSize < MinBufferSize)
        {
            bufferSize = MinBufferSize;
        }

        BufferSize = bufferSize;
        //_buffer = new byte[bufferSize];
        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        _isConcatenationNeeded = isConcatenationNeeded;
        _logger = logger;
    }

    public async IAsyncEnumerable<string[]> ReadAllLinesBulkAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int bytesRead;
        if (stream.Length < MinBufferSize)
        {
            _logger.Information($"Do not use {nameof(BulkTextReader)} with small streams. It's inefficient.");
            using TextReader tr = new StreamReader(stream);
            var strings = await tr.ReadToEndAsync().ConfigureAwait(false);
            yield return strings
                .Split('\n')
                .Select(s => s.TrimEnd('\r'))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
            yield break;
        }

        
        var bufferedStream = new BufferedStream(stream, BufferSize);
        using var memoryStream = new MemoryStream(_buffer, false);
        using var streamReader = new StreamReader(memoryStream);
        while ((bytesRead = await bufferedStream.ReadAsync(_buffer, 0, BufferSize, cancellationToken)) != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Lines.Clear();

            memoryStream.Seek(0, SeekOrigin.Begin);
            if (bytesRead < BufferSize)
            {
                //on a last read the buffer may still contain data from the previous read and it need to be cleared.
                Array.Clear(_buffer, bytesRead, _buffer.Length - bytesRead);
            }

            var firstReadInNewBuffer = true;
            while (!streamReader.EndOfStream)
            {
                var line = await streamReader.ReadLineAsync();
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }

                //if the line is the last one in buffer (but not the only one)
                if (streamReader.EndOfStream && !firstReadInNewBuffer)
                {
                    //save last line of the buffer - it may be appended to when reading the next buffer
                    if (line.StartsWith('\0'))
                    {
                        break; //HACK: for when readers returns rest of the buffer (filled with '\0')
                        //if the last character in stream was '\n'
                    }

                    _lineBuilder.Append(line);
                    break;
                }

                if (firstReadInNewBuffer)
                {
                    firstReadInNewBuffer = false;
                    if (_lineBuilder.Length > 0)
                    {
                        var oldLine = _lineBuilder.ToString();
                        _lineBuilder.Clear();
                        // if old line or new line is not in a correct format, we merge them
                        if (_isConcatenationNeeded(oldLine, line))
                        {
                            var result = oldLine + line;
                            //yield return new DataLineWithSeparator(result, result.IndexOf('.'));
                            Lines.Add(result);
                            continue;
                        }

                        // if both lines are correct - we return both of them
                        Lines.Add(oldLine);
                        Lines.Add(line);
                        continue;
                    }
                }

                Lines.Add(line);
            }

            yield return Lines.ToArray();
            _logger.Verbose("returned next bulk of {LinesCount}", Lines.Count);
            Lines.Clear();
        }

        //last line will nothing to append to it, so we return it as is
        if (bytesRead == 0 && _lineBuilder.Length > 0)
        {
            var line = _lineBuilder.ToString();
            _lineBuilder.Clear();
            yield return new[] { line };
        }
    }

    public async IAsyncEnumerable<string> ReadAllLinesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.Debug($"started reading stream");
        await foreach (var array in ReadAllLinesBulkAsync(stream, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var line in array)
            {
                yield return line;
            }
        }

        _logger.Debug($"ended reading stream");
    }

    public void Reset()
    {
        _lineBuilder.Clear();
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}