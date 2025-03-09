namespace FmFileParse.SaveImport;

internal class DataFileFact(
    DataFileType type,
    string name,
    int dataSize,
    int stringLength,
    DataFileHeaderInformation? headerOverload = null)
{
    public DataFileType Type { get; } = type;

    public string Name { get; } = name;

    public int DataSize { get; } = dataSize;

    public int StringLength { get; } = stringLength;

    public DataFileHeaderInformation? HeaderOverload { get; } = headerOverload;
}
