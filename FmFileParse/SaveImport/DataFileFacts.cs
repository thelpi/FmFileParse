namespace FmFileParse.SaveImport;

internal static class DataFileFacts
{
    public static List<DataFileFact> GetDataFileFacts()
    {
        var facts = new List<DataFileFact>
        {
            new(DataFileType.FirstNames, "first_names.dat", 60, 50),
            new(DataFileType.SecondNames, "second_names.dat", 60, 50),
            new(DataFileType.CommonNames, "common_names.dat", 60, 50),
            new(DataFileType.Staff, "staff.dat", 110, 0),
            new(DataFileType.Players, "player.dat", 70, 0),
            new(DataFileType.Clubs, "club.dat", 581, 0),
            new(DataFileType.ClubComps, "club_comp.dat", 107, 0),
            new(DataFileType.Nations, "nation.dat", 290, 0),
            new(DataFileType.General, "general.dat", 3952, 0)
        };

        var headerInfo = new DataFileHeaderInformation(0, 4, 8, 21, 17);
        facts.Add(new DataFileFact(DataFileType.Contracts, "contract.dat", 80, 0, headerOverload: headerInfo));

        return facts;
    }

    public static DataFileFact GetDataFileFact(string name)
    {
        var matchingFacts = GetDataFileFacts().FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        return matchingFacts ?? new DataFileFact(DataFileType.Unknown, name, 0, 0);
    }
}
