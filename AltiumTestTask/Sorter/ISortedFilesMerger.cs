namespace MergeSortTestTask.Sorter;

/// <summary>
/// Merges already sorted files into one.
/// </summary>
public interface ISortedFilesMerger
{
    /// <summary>
    /// Merges already sorted files into one.
    /// </summary>
    /// <param name="initiallySortedFiles">List of paths to sorted temporary files.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> that may be used
    /// to stop processing of this task.</param>
    /// <returns>Path to the result file.</returns>
    Task<string> MergeFilesAsync(string[] initiallySortedFiles, CancellationToken cancellationToken);
}