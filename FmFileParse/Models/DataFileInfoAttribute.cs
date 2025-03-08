namespace FmFileParse.Models;

public class DataFileInfoAttribute : Attribute
{
    public int DataFilePosition;

    public bool IsIntrinsic;

    public int Length;

    public DataFileInfoAttribute(int position, int length = 0, bool isIntrinsic = false)
    {
        DataFilePosition = position;
        IsIntrinsic = isIntrinsic;
        Length = length;
    }
}
