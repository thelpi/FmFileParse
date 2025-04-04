using FmFileParse.Models.Attributes;

namespace FmFileParse.Models;

public class Confederation : BaseData
{
    [DataPosition(4, Length = 25)]
    public string ContinentName { get; set; } = string.Empty;

    [DataPosition(61, Length = 100)]
    public string Name { get; set; } = string.Empty;

    [DataPosition(163, Length = 10)]
    public string Acronym { get; set; } = string.Empty;

    [DataPosition(190)]
    public decimal Strength { get; set; }
}
