namespace FmFileParse.SaveImport;

internal class DataFileHeaderInformation
{
    public byte AdditionalHeaderIndicatorPosition { get; }

    public byte InitialNumberOfRecordsPosition { get; }

    public byte MinimumHeaderLength { get; }

    public byte ExtraHeaderLength { get; }

    public byte FurtherNumberOfRecordsPosition { get; }

    public DataFileHeaderInformation(byte additionalHeaderIndicatorPosition, byte initialNumberOfRecordsPosition, byte minimumHeaderLength, byte extraHeaderLength, byte furtherNumberOfRecordsPosition)
    {
        AdditionalHeaderIndicatorPosition = additionalHeaderIndicatorPosition;
        InitialNumberOfRecordsPosition = initialNumberOfRecordsPosition;
        MinimumHeaderLength = minimumHeaderLength;
        ExtraHeaderLength = extraHeaderLength;
        FurtherNumberOfRecordsPosition = furtherNumberOfRecordsPosition;
    }
}
