﻿using System.Text;
using FmFileParse.Models.Internal;

namespace FmFileParse.SaveImport;

internal static class ByteHandler
{
    private static readonly Encoding DefaultEncoding = Encoding.Latin1;

    public static short GetShortFromBytes(this byte[] bytes, int start)
        => BitConverter.ToInt16(bytes.Skip(start).Take(2).ToArray(), 0);

    public static int GetIntFromBytes(this byte[] bytes, int start)
        => BitConverter.ToInt32(bytes.Skip(start).Take(4).ToArray(), 0);

    public static decimal GetDecimalFromBytes(this byte[] bytes, int start)
        => (decimal)BitConverter.ToDouble(bytes.Skip(start).Take(8).ToArray(), 0);

    public static string GetStringFromBytes(this byte[] bytes, int start, int length = 0)
        => DefaultEncoding.GetString(TrimEnd(bytes.Skip(start).Take(length > 0 ? length : bytes.Length).ToArray()));

    public static DateTime? GetDateFromBytes(this byte[] bytes, int start)
        => ConvertToDate(bytes.Skip(start).Take(5).ToArray());

    public static byte GetByteFromBytes(this byte[] bytes, int start)
        => bytes[start];

    public static List<byte[]> GetAllDataFromFile(this DataFile dataFile, string fileName, int sizeOfData)
    {
        using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        var numberOfRecords = dataFile.GetNumberOfRecordsFromDataFile(sizeOfData, br, out var startReadPosition);

        br.BaseStream.Seek(startReadPosition, SeekOrigin.Begin);

        var records = new List<byte[]>(numberOfRecords);
        for (var i = 0; i < numberOfRecords; i++)
        {
            var buffer = new byte[sizeOfData];
            br.BaseStream.Read(buffer, 0, sizeOfData);
            records.Add(buffer);
        }

        return records;
    }

    private static byte[] TrimEnd(byte[] array)
    {
        var lastIndex = Array.FindIndex(array, b => b == 0);

        if (lastIndex >= 0)
        {
            Array.Resize(ref array, lastIndex);
        }

        return array;
    }

    private static DateTime? ConvertToDate(byte[] bytes)
    {
        var day = BitConverter.ToInt16(bytes, 0);
        var year = BitConverter.ToInt16(bytes, 2);

        return year <= 0
            ? null
            : new DateTime(year, 1, 1).AddDays(day);
    }

    private static int GetNumberOfRecordsFromDataFile(this DataFile dataFile, int sizeOfData, BinaryReader br, out int startReadPosition)
    {
        var numberOfRecords = dataFile.Length / sizeOfData;
        startReadPosition = dataFile.Position;

        if (dataFile.FileFacts.HeaderOverload is not null)
        {
            var header = new byte[dataFile.FileFacts.HeaderOverload.MinimumHeaderLength];
            br.BaseStream.Seek(startReadPosition, SeekOrigin.Begin);
            br.BaseStream.Read(header, 0, dataFile.FileFacts.HeaderOverload.MinimumHeaderLength);
            startReadPosition += dataFile.FileFacts.HeaderOverload.MinimumHeaderLength;

            var numberHeaderRows = GetIntFromBytes(header, dataFile.FileFacts.HeaderOverload.AdditionalHeaderIndicatorPosition);
            numberOfRecords = GetIntFromBytes(header, dataFile.FileFacts.HeaderOverload.InitialNumberOfRecordsPosition);
            var furtherNumberOfRecords = 0;

            if (numberHeaderRows > 0)
            {
                for (var headerLoop = 0; headerLoop < numberHeaderRows; headerLoop++)
                {
                    header = new byte[dataFile.FileFacts.HeaderOverload.ExtraHeaderLength];
                    br.BaseStream.Seek(startReadPosition, SeekOrigin.Begin);
                    br.BaseStream.Read(header, 0, dataFile.FileFacts.HeaderOverload.ExtraHeaderLength);
                    startReadPosition += dataFile.FileFacts.HeaderOverload.ExtraHeaderLength;
                }
                furtherNumberOfRecords = GetIntFromBytes(header, dataFile.FileFacts.HeaderOverload.FurtherNumberOfRecordsPosition);
            }
            numberOfRecords = furtherNumberOfRecords > 0 ? furtherNumberOfRecords : numberOfRecords;
        }

        return numberOfRecords;
    }
}
