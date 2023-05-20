namespace AltiumTestTask.Sorter;

public class DataLineWithSeparator
{
    public string Data;
    public int separatorPosition;

    public DataLineWithSeparator(string data, int separatorPosition)
    {
        Data = data;
        this.separatorPosition = separatorPosition;
    }
}