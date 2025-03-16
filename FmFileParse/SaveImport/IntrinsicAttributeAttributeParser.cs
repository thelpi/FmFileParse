using System.Reflection;
using FmFileParse.Models;
using FmFileParse.Models.Attributes;

namespace FmFileParse.SaveImport;

internal static class IntrinsicAttributeAttributeParser
{
    private const int HighConversion = 20;
    private const int LowConversion = 200;

    private static readonly (PropertyInfo property, IntrinsicAttributeAttribute attribute)[] _intrinsicAttributes = typeof(Player)
        .GetProperties()
        .Select(x => (Property: x, Attribute: x.GetCustomAttribute<IntrinsicAttributeAttribute>()))
        .Where(x => x.Attribute is not null)
        .Select(x => (x.Property, x.Attribute!))
        .ToArray();

    // note: 'CurrentAbility' and 'GoalKeeperPos' must be known beforehand
    internal static Player ComputeAndSetIntrinsicAttributes(this Player player)
    {
        foreach (var (property, attribute) in _intrinsicAttributes)
        {
            var intrinsicValue = (sbyte)(byte)property.GetValue(player)!;
            var inGameValue = intrinsicValue.IntrisincToInGameAttributeValue(attribute.Type, player.CurrentAbility, player.GoalKeeperPos);
            property.SetValue(player, inGameValue);
        }
        return player;
    }

    private static byte IntrisincToInGameAttributeValue(
        this sbyte intrinsicValue,
        IntrinsicType intrinsicType,
        short currentAbility,
        byte goalKeeperRate)
    {
        var isGk = goalKeeperRate >= Settings.MinAttributeForPosition;

        return intrinsicType switch
        {
            IntrinsicType.GoalkeeperAttribute => intrinsicValue.Convert(currentAbility, isGk ? HighConversion : LowConversion),
            IntrinsicType.FieldPlayerAttribute => intrinsicValue.Convert(currentAbility, isGk ? LowConversion : HighConversion),
            _ => intrinsicValue.Convert(currentAbility, HighConversion),
        };
    }

    private static byte Convert(this sbyte intrinsicValue, short currentAbility, double typeCoefficient)
    {
        var d = (intrinsicValue / 10.0) + (currentAbility / typeCoefficient) + 10;

        var r = (d * d / 30.0) + (d / 3.0) + 0.5;

        if (r < Settings.MinAttributeValue)
        {
            r = Settings.MinAttributeValue;
        }
        else if (r > Settings.MaxAttributeValue)
        {
            r = Settings.MaxAttributeValue;
        }

        return (byte)Math.Truncate(r);
    }
}
