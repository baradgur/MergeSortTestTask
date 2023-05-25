using System.Collections.Immutable;

namespace AltiumTestTask.Sorter;

/// <summary>
///     Reading strings in bulk from a large stream.
/// </summary>
public interface IBulkTextReader
{
    public int BufferSize { get; }

    /// <summary>
    /// One yield returns a bulk of lines contained in the reading buffer.
    /// One yield roughly corresponds to one i/o operation.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for this task.</param>
    /// <returns>Array of strings.</returns>
    public IAsyncEnumerable<string[]> ReadAllLinesBulkAsync(Stream stream, CancellationToken cancellationToken);

    //TODO: try ReadOnlyMemory<char>
    /// <summary>
    /// Returns enumeration of all lines in stream, reading a buffer from stream whenever necessary.    
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<string> ReadAllLinesAsync(Stream stream, CancellationToken cancellationToken);
}

/// <summary>
///     Reading strings in bulk from a large stream.
/// </summary>
public interface ILineReader
{
    public int BufferSize { get; }

    /// <summary>
    /// Returns enumeration of all lines in stream, reading a buffer from stream whenever necessary.    
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to read from.</param>
    /// <param name="buffer">Buffer to read to.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for this task.</param>
    /// <returns></returns>
    public IAsyncEnumerable<ReadOnlyMemory<char>> ReadAllLinesAsync(Stream stream, ReadOnlyMemory<char> buffer, CancellationToken cancellationToken);
}