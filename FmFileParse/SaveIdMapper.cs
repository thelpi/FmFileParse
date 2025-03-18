namespace FmFileParse;

internal readonly struct SaveIdMapper
{
    public string Key { get; init; }

    public int DbId { get; init; }

    public Dictionary<int, int> SaveId { get; init; }
}
