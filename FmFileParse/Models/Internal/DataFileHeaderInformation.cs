namespace FmFileParse.Models.Internal;

internal class DataFileHeaderInformation(
    byte additionalHeaderIndicatorPosition,
    byte initialNumberOfRecordsPosition,
    byte minimumHeaderLength,
    byte extraHeaderLength,
    byte furtherNumberOfRecordsPosition)
{
    public byte AdditionalHeaderIndicatorPosition { get; } = additionalHeaderIndicatorPosition;

    public byte InitialNumberOfRecordsPosition { get; } = initialNumberOfRecordsPosition;

    public byte MinimumHeaderLength { get; } = minimumHeaderLength;

    public byte ExtraHeaderLength { get; } = extraHeaderLength;

    public byte FurtherNumberOfRecordsPosition { get; } = furtherNumberOfRecordsPosition;
}
