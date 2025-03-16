using System.Reflection;
using FmFileParse.Models;
using FmFileParse.Models.Attributes;

namespace FmFileParse.SaveImport;

internal static class DataPositionAttributeParser
{
    private static readonly Dictionary<Type, List<(PropertyInfo, DataPositionAttribute?)>> _reflectionCache = [];

    internal static void SetDataPositionableProperties<T>(T data, byte[] binaryContent)
    {
        if (!_reflectionCache.TryGetValue(typeof(T), out var propsWithAttr))
        {
            propsWithAttr = typeof(T)
                .GetProperties()
                .Select(p => (p, p.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(DataPositionAttribute)) as DataPositionAttribute))
                .ToList();
            _reflectionCache.Add(typeof(T), propsWithAttr);
        }

        foreach (var (p, attr) in propsWithAttr)
        {
            if (attr is null || (p.DeclaringType != typeof(T) && p.DeclaringType != typeof(BaseData)))
            {
                continue;
            }

            object? propValue = null;
            if (p.PropertyType == typeof(byte) || p.PropertyType == typeof(byte?))
            {
                propValue = ByteHandler.GetByteFromBytes(binaryContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?))
            {
                propValue = ByteHandler.GetByteFromBytes(binaryContent, attr.StartAt) == 1;
            }
            else if (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
            {
                propValue = ByteHandler.GetDateFromBytes(binaryContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(int) || p.PropertyType == typeof(int?))
            {
                propValue = ByteHandler.GetIntFromBytes(binaryContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(short) || p.PropertyType == typeof(short?))
            {
                propValue = ByteHandler.GetShortFromBytes(binaryContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?))
            {
                propValue = ByteHandler.GetDecimalFromBytes(binaryContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(string))
            {
                propValue = ByteHandler.GetStringFromBytes(binaryContent, attr.StartAt, attr.Length);
            }
            else
            {
                throw new NotSupportedException("That type of attribute is not managed!");
            }

            p.SetValue(data, propValue);
        }
    }
}
