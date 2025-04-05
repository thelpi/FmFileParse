using System.Reflection;
using FmFileParse.Models;
using FmFileParse.Models.Attributes;

namespace FmFileParse.SaveImport;

internal static class IntrinsicAttributeAttributeParser
{
    private const int HighConversion = 20;
    private const int LowConversion = 200;
    private const int MinAttributeValue = 1;
    private const int MinAttributeForPosition = 15;

    internal const int MaxAttributeValue = 20;

    private static readonly (PropertyInfo property, IntrinsicAttributeAttribute attribute)[] _intrinsicAttributes = typeof(Player)
        .GetProperties()
        .Select(x => (Property: x, Attribute: x.GetCustomAttribute<IntrinsicAttributeAttribute>()))
        .Where(x => x.Attribute is not null)
        .Select(x => (x.Property, x.Attribute!))
        .ToArray();

    internal static (byte, byte) ConvertAttributeIntrinsicValue(this Player player, string propertyName)
    {
        var propMatch = _intrinsicAttributes.FirstOrDefault(p => p.property.Name == propertyName);
        if (propMatch.Equals(default))
        {
            throw new ArgumentException("The specified property is not intrinsic.", nameof(propertyName));
        }

        var intrinsicValue = (sbyte)(byte)propMatch.property.GetValue(player)!;
        return intrinsicValue.IntrisincToInGameAttributeValue(propMatch.attribute.Type, player.CurrentAbility, player.PotentialAbility, player.GoalKeeperPos);
    }

    private static (byte, byte) IntrisincToInGameAttributeValue(
        this sbyte intrinsicValue,
        IntrinsicType intrinsicType,
        short currentAbility,
        short potentialAbility,
        byte goalKeeperRate)
    {
        var isGk = goalKeeperRate >= MinAttributeForPosition;

        return intrinsicType switch
        {
            IntrinsicType.GoalkeeperAttribute =>
                (intrinsicValue.Convert(currentAbility, isGk ? HighConversion : LowConversion),
                intrinsicValue.Convert(potentialAbility, isGk ? HighConversion : LowConversion)),
            IntrinsicType.FieldPlayerAttribute =>
                (intrinsicValue.Convert(currentAbility, isGk ? LowConversion : HighConversion),
                intrinsicValue.Convert(potentialAbility, isGk ? LowConversion : HighConversion)),
            _ =>
                (intrinsicValue.Convert(currentAbility, HighConversion),
                intrinsicValue.Convert(potentialAbility, HighConversion)),
        };
    }

    private static byte Convert(this sbyte intrinsicValue, short ability, double typeCoefficient)
    {
        var d = (intrinsicValue / 10.0) + (ability / typeCoefficient) + 10;

        var r = (d * d / 30.0) + (d / 3.0) + 0.5;

        if (r < MinAttributeValue)
        {
            r = MinAttributeValue;
        }
        else if (r > MaxAttributeValue)
        {
            r = MaxAttributeValue;
        }

        return (byte)Math.Truncate(r);
    }
}
