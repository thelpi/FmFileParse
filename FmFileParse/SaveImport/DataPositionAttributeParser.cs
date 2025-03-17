using System.Reflection;
using FmFileParse.Models;
using FmFileParse.Models.Attributes;

namespace FmFileParse.SaveImport;

internal static class DataPositionAttributeParser
{
    private static readonly Dictionary<Type, List<(PropertyInfo, DataPositionAttribute?, bool)>> _reflectionCache = [];

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
            if (p.PropertyType == typeof(byte) || p.PropertyType == typeof(byte?))
            {
                var sourceValue = binaryContent.GetByteFromBytes(attr.StartAt);
                propValue = reversed ? (byte)(Settings.MaxAttributeValue - sourceValue) : sourceValue;
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
}
