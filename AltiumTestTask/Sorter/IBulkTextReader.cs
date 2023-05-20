using System.Collections.Immutable;

namespace AltiumTestTask.Sorter;

/// <summary>
///     For reading strings in bulk from a large stream.
/// </summary>
public interface IBulkTextReader
{
    public int BufferSize { get; }
    /// <summary>
    /// One yield returns s bulk of lines contained in the reading buffer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Array of strings.</returns>
    public IEnumerable<ImmutableArray<string>> ReadAllLinesBulk(Stream stream);
}