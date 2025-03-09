using System.Reflection;
using FmFileParse.Models.Attributes;

namespace FmFileParse.SaveImport;

internal static class DataPositionAttributeParser
{
    internal static void SetDataPositionableProperties<T>(T data, byte[] binaryContent)
    {
        var propsWithAttr = typeof(T)
            .GetProperties()
            .Select(p => (p, p.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(DataPositionAttribute)) as DataPositionAttribute))
            .ToList();

        foreach (var (p, attr) in propsWithAttr)
        {
            if (attr is null)
            {
                continue;
            }

            var isDateProperty = p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?);

            object? propValue = null;
            if (p.PropertyType == typeof(byte) || p.PropertyType == typeof(byte?))
            {
                propValue = ByteHandler.GetByteFromBytes(binaryContent, attr.StartAt);
            }
            else if (p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?))
            {
                propValue = ByteHandler.GetByteFromBytes(binaryContent, attr.StartAt) == 1;
            }
            else if (isDateProperty)
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
            else if (p.PropertyType == typeof(string))
            {
                propValue = ByteHandler.GetStringFromBytes(binaryContent, attr.StartAt, attr.Length);
            }
            else
            {
                throw new NotSupportedException("That type of attribute is not managed!");
            }

            propValue ??= isDateProperty
                ? (DateTime.TryParse(attr.Default?.ToString(), out var dateTime)
                    ? dateTime
                    : null)
                : attr.Default;

            p.SetValue(data, propValue);
        }
    }
}
