namespace FmFileParse.Models.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal class DataPositionAttribute(int startAt) : Attribute
{
    public int StartAt { get; } = startAt;

    /// <summary>
    /// String only
    /// </summary>
    public int Length { get; init; }
}
