namespace AltiumTestTask.Sorter;

/// <summary>
///     Separator for a large filestream. Creates to several sorted temporary files.
/// </summary>
public interface ILargeFileSeparatorSorter
{
    //size of the buffer to use when reading
    int BufferSize { get; init; }
    /// <summary>
    ///     Separates a large filestream to several temporary files.
    /// </summary>
    /// <param name="fileStream">Filestream of a large file.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that may be used to stop processing of the task.</param>
    /// <returns>Collection of paths to temporary files containing sorted data form original filestream.</returns>
    Task<string[]> SeparateToSortedFilesAsync(FileStream fileStream, CancellationToken cancellationToken);
}