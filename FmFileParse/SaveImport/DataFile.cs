namespace FmFileParse.SaveImport;

internal class DataFile
{
    public DataFileFact FileFacts { get; }

    public int Position { get; }

    public int Length { get; }

    public DataFile(DataFileFact fileFacts, int position, int length)
    {
        FileFacts = fileFacts ?? new DataFileFact(DataFileType.General, string.Empty, 0, 0);
        Position = position;
        Length = length;
    }

    public override string ToString()
    {
        return $"{FileFacts.Name} [{FileFacts.Type}] ({Position}/{Length})";
    }
}
