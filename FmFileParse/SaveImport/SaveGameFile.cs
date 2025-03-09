namespace FmFileParse.SaveImport;

internal class SaveGameFile
{
    public string FileName { get; set; }

    public bool IsCompressed { get; set; }

    public DateTime GameDate { get; set; }

    public List<DataFile> DataBlockNameList { get; set; }

    public SaveGameFile()
    {
        DataBlockNameList = new List<DataFile>();
    }
}
