namespace MergeSortTestTask.Sorter;

public ref struct DataLineWrapper
{
    public ReadOnlySpan<char> FirstPart;
    public ReadOnlySpan<char> SecondPart;
}