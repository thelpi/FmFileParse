using FmFileParse.Models;
using static FmFileParse.Converters.NationTupleConverter;

namespace FmFileParse.Converters
{
    internal static class ConverterIdFactory
    {
        public static ITupleConverter<T> CreateTupleConverter<T>() where T : class
        {
            if (typeof(T) == typeof(Staff))
            {
                return (ITupleConverter<T>)new StaffTupleConverter();
            }

            if (typeof(T) == typeof(Club))
            {
                return (ITupleConverter<T>)new ClubTupleConverter();
            }
            
            if (typeof(T) == typeof(Country))
            {
                return (ITupleConverter<T>)new NationTupleConverter();
            }
            
            throw new NotImplementedException("No Class Converter");
        }
    }

    internal static class ConverterFactory
    {
        public static ICMConverter<T> CreateConverter<T>()
        {
            if (typeof(T) == typeof(Player))
            {
                return (ICMConverter<T>)new PlayerConverter();
            }

            throw new NotImplementedException("Unknown Object Converter Needed");
        }
    }
}
