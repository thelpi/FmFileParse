namespace FmFileParse.SaveImport;

internal class SaveGameFile
{
    public string FileName { get; set; } = string.Empty;

    public bool IsCompressed { get; set; }

    public DateTime GameDate { get; set; }

    public List<DataFile> DataBlockNameList { get; set; } = [];
}
