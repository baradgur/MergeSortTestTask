namespace MergeSortTestTask.Sorter;

/// <summary>
/// Separates the incoming stream to sorted files.
/// </summary>
public interface ISortingSeparator
{
    /// <summary>
    /// Separates the incoming stream to sorted files.
    /// </summary>
    /// <param name="fs">Incoming stream.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> that may be used
    /// to stop processing of this task.</param>
    /// <returns>A collection of the paths to files containing the stream content.</returns>
    Task<string[]> SeparateAndSortAsync(FileStream fs, CancellationToken cancellationToken);
}