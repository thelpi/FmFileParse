using System.Reflection;
using FmFileParse.Models;

namespace FmFileParse.Converters;

internal class NationTupleConverter : ITupleConverter<Country>
{
    Tuple<int, object> ITupleConverter<Country>.Convert(byte[] source)
    {
        var nation = new Country();
        ConverterReflection.SetConversionProperties(nation, source);
        return new Tuple<int, object>(nation.Id, nation);
    }
    public class StaffTupleConverter : ITupleConverter<Staff>
    {
        Tuple<int, object> ITupleConverter<Staff>.Convert(byte[] source)
        {
            var staff = new Staff();

            PropertyInfo[] props = staff.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            DataFileInfoAttribute[] attribs = new DataFileInfoAttribute[props.Length];

            for (int i = 0; i < attribs.Length; i++)
            {
                attribs[i] = (DataFileInfoAttribute)props[i].GetCustomAttributes(typeof(DataFileInfoAttribute), true).FirstOrDefault();
            }

            ConverterReflection.SetConversionProperties(staff, source);
            //ConverterReflection.SetConversionProperties(staff, props, attribs, source);

            return new Tuple<int, object>(staff.StaffPlayerId, staff);
        }
    }
    internal class ClubTupleConverter : ITupleConverter<Club>
    {
        private static byte[] bytes;

        Tuple<int, object> ITupleConverter<Club>.Convert(byte[] sourceOfData)
        {
            bytes = sourceOfData;
            Club club = new Club();

            PropertyInfo[] props = club.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            DataFileInfoAttribute[] attribs = new DataFileInfoAttribute[props.Length];

            if (props == null)
            {
                props = typeof(Club).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                attribs = new DataFileInfoAttribute[props.Length];

                for (int i = 0; i < attribs.Length; i++)
                {
                    attribs[i] = (DataFileInfoAttribute)props[i].GetCustomAttributes(typeof(DataFileInfoAttribute), true).FirstOrDefault();
                }
            }
            
            ConverterReflection.SetConversionProperties(club, /*props, attribs,*/ bytes);

            var result = new Tuple<int, object>(club.Id, club);
            return result;
        }
    }
}

internal class PlayerConverter : ICMConverter<Player>
{
    public Player Convert(byte[] source)
    {
        var player = new Player();
        ConverterReflection.SetConversionProperties(player, source);
        return player;
    }
}
internal static class ConverterReflection
{
    public static void SetConversionProperties(object target, byte[] source)
    {
        foreach (var prop in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var positionAttribute = (DataFileInfoAttribute)prop.GetCustomAttributes(typeof(DataFileInfoAttribute), true).FirstOrDefault();

            if (positionAttribute != null)
            {
                prop.SetValue(target, ByteHandler.GetObjectFromBytes(source, positionAttribute.DataFilePosition, prop.PropertyType, positionAttribute.Length, positionAttribute.IsIntrinsic));
            }
        }
    }
    public static void SetConversionProperties(object target, PropertyInfo[] props, DataFileInfoAttribute[] attribs, byte[] source)
    {
        for (int i = 0; i < props.Length; i++)
        {
            var prop = props[i];
            var positionAttribute = attribs[i];

            if (prop != null && positionAttribute != null)
            {
                prop.SetValue(target, ByteHandler.GetObjectFromBytes(source, positionAttribute.DataFilePosition, prop.PropertyType, positionAttribute.Length, positionAttribute.IsIntrinsic));
            }
        }
    }
}
