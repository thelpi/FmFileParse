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

    /// <summary>
    /// For <see cref="DateTime"/>, use a string representation.
    /// </summary>
    public object? Default { get; init; }
}
