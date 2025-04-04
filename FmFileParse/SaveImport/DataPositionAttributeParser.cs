using System.Reflection;
using FmFileParse.Models;
using FmFileParse.Models.Attributes;

namespace FmFileParse.SaveImport;

internal static class DataPositionAttributeParser
{
    private static readonly Dictionary<Type, List<(PropertyInfo, DataPositionAttribute?, bool)>> _reflectionCache = [];

    private static readonly Dictionary<TransferStatus, Func<byte, bool>> _transferStatusOperations = new()
    {
         { TransferStatus.TransferListedByClub, x => (x & 1) == 1 },
         { TransferStatus.TransferListedByRequest, x => (x & 8) == 8 },
         { TransferStatus.ListedForLoan, x => (x & 2) == 2 },
         { TransferStatus.Unknown, x => !((x & 1) == 1 || (x & 8) == 8 ||  (x & 2) == 2) }
    };

    private static readonly Dictionary<SquadStatus, Func<byte, bool>> _squadStatusOperations = new()
    {
         { SquadStatus.Uncertain, x => (x & 240) == 0 },
         { SquadStatus.Indispensable, x => (x & 224) == 0 },
         { SquadStatus.Important, x => (x & 208) == 0 },
         { SquadStatus.SquadRotation, x => (x & 192) == 0 },
         { SquadStatus.Backup, x => (x & 176) == 0 },
         { SquadStatus.HotProspect, x => (x & 160) == 0 },
         { SquadStatus.DecentYoung, x => (x & 144) == 0 },
         { SquadStatus.NotNeeded, x => (x & 128) == 0 },
         { SquadStatus.OnTrial, x => (x & 112) == 0 },
    };

    internal static T SetDataPositionableProperties<T>(this T data, string stringContent)
    {
        if (!_reflectionCache.TryGetValue(typeof(T), out var propsWithAttr))
        {
            propsWithAttr = typeof(T)
                .GetProperties()
                .Select(p => (p, p.GetCustomAttribute<DataPositionAttribute>(), p.GetCustomAttribute<ReversedAttributeAttribute>() is not null))
                .ToList();
            _reflectionCache.Add(typeof(T), propsWithAttr);
        }

        foreach (var (p, attr, reversed) in propsWithAttr)
        {
            if (attr is null || (p.DeclaringType != typeof(T) && p.DeclaringType != typeof(BaseData)))
            {
                continue;
            }

            object? propValue = null;
            if (p.PropertyType == typeof(byte) || p.PropertyType == typeof(byte?))
            {
                var sourceValue = StringHandler.ByteGet(stringContent, attr.StartAt);
                propValue = reversed ? (byte)(IntrinsicAttributeAttributeParser.MaxAttributeValue - sourceValue) : sourceValue;
            }
            else if (p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?))
            {
                propValue = StringHandler.BoolGet(stringContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
            {
                propValue = StringHandler.DateGet(stringContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(int) || p.PropertyType == typeof(int?))
            {
                propValue = StringHandler.IntGet(stringContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(short) || p.PropertyType == typeof(short?))
            {
                propValue = StringHandler.ShortGet(stringContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?))
            {
                propValue = (decimal)StringHandler.DoubleGet(stringContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(string))
            {
                propValue = StringHandler.StringGet(stringContent, attr.StartAt, attr.Length);
            }
            else
            {
                throw new NotSupportedException("That type of attribute is not managed!");
            }

            p.SetValue(data, propValue);
        }

        return data;
    }

    internal static T SetDataPositionableProperties<T>(this T data, byte[] binaryContent)
    {
        if (!_reflectionCache.TryGetValue(typeof(T), out var propsWithAttr))
        {
            propsWithAttr = typeof(T)
                .GetProperties()
                .Select(p => (p, p.GetCustomAttribute<DataPositionAttribute>(), p.GetCustomAttribute<ReversedAttributeAttribute>() is not null))
                .ToList();
            _reflectionCache.Add(typeof(T), propsWithAttr);
        }

        foreach (var (p, attr, reversed) in propsWithAttr)
        {
            if (attr is null || (p.DeclaringType != typeof(T) && p.DeclaringType != typeof(BaseData)))
            {
                continue;
            }

            object? propValue = null;
            if (p.PropertyType == typeof((byte, byte)))
            {
                var sourceValue = binaryContent.GetByteFromBytes(attr.StartAt);
                propValue = (sourceValue, sourceValue);
            }
            else if (p.PropertyType == typeof(TransferStatus) || p.PropertyType == typeof(TransferStatus?))
            {
                var sourceValue = binaryContent.GetByteFromBytes(attr.StartAt);
                propValue = sourceValue.ToTransferStatus();
            }
            else if (p.PropertyType == typeof(SquadStatus) || p.PropertyType == typeof(SquadStatus?))
            {
                var sourceValue = binaryContent.GetByteFromBytes(attr.StartAt);
                propValue = sourceValue.ToSquadStatus();
            }
            else if (p.PropertyType == typeof(byte) || p.PropertyType == typeof(byte?))
            {
                var sourceValue = binaryContent.GetByteFromBytes(attr.StartAt);
                propValue = reversed ? (byte)(IntrinsicAttributeAttributeParser.MaxAttributeValue - sourceValue) : sourceValue;
            }
            else if (p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?))
            {
                propValue = binaryContent.GetByteFromBytes(attr.StartAt) == 1;
            }
            else if (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
            {
                propValue = binaryContent.GetDateFromBytes(attr.StartAt);
            }
            else if (p.PropertyType == typeof(int) || p.PropertyType == typeof(int?))
            {
                propValue = binaryContent.GetIntFromBytes(attr.StartAt);
            }
            else if (p.PropertyType == typeof(short) || p.PropertyType == typeof(short?))
            {
                propValue = binaryContent.GetShortFromBytes(attr.StartAt);
            }
            else if (p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?))
            {
                propValue = binaryContent.GetDecimalFromBytes(attr.StartAt);
            }
            else if (p.PropertyType == typeof(string))
            {
                propValue = binaryContent.GetStringFromBytes(attr.StartAt, attr.Length);
            }
            else
            {
                throw new NotSupportedException("That type of attribute is not managed!");
            }

            p.SetValue(data, propValue);
        }

        return data;
    }

    private static TransferStatus? ToTransferStatus(this byte statusByte)
    {
        foreach (var (transferStatus, func) in _transferStatusOperations)
        {
            if (func(statusByte))
            {
                return transferStatus;
            }
        }

        return null;
    }

    private static SquadStatus? ToSquadStatus(this byte statusByte)
    {
        foreach (var (squadStatus, func) in _squadStatusOperations)
        {
            if (func(statusByte))
            {
                return squadStatus;
            }
        }

        return null;
    }
}
