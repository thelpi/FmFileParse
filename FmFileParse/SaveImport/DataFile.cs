namespace FmFileParse.SaveImport;

internal class DataFile(
    DataFileFact fileFacts,
    int position,
    int length)
{
    public DataFileFact FileFacts { get; } = fileFacts ?? new DataFileFact(DataFileType.General, string.Empty, 0, 0);

    public int Position { get; } = position;

    public int Length { get; } = length;

    public override string ToString()
        => $"{FileFacts.Name} [{FileFacts.Type}] ({Position}/{Length})";
}
