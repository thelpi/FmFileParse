namespace FmFileParse.Models.Attributes;

internal class DataPositionAttribute : Attribute
{
    public DataPositionAttribute(int startAt)
    {
        StartAt = startAt;
    }

    public int StartAt { get; }

    /// <summary>
    /// String only
    /// </summary>
    public int Length { get; init; }
}
