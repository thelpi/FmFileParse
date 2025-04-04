using FmFileParse.Models.Internal;

namespace FmFileParse.Models.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal class DataPositionAttribute(int startAt) : Attribute
{
    public int StartAt { get; } = startAt;

    /// <summary>
    /// String only
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// Indicates the attribute is readable on db file, save file, or both (default).
    /// </summary>
    public PositionAttributeFileTypes FileType { get; init; } = PositionAttributeFileTypes.Both;
}
