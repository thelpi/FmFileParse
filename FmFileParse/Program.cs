using FmFileParse;
using FmFileParse.Models;
using FmFileParse.Models.Internal;
using FmFileParse.SaveImport;

Console.WriteLine("0 - test the save file reader");
Console.WriteLine("1 - data importation");
Console.WriteLine("Other - exit");

var rawChoice = Console.ReadLine();

if (!int.TryParse(rawChoice, out var choice) || (choice != 0 && choice != 1))
{
    Console.WriteLine("No action to do; Press any key to close.");
    Console.ReadKey();
    return;
}

var saveFiles = Directory.GetFiles(Settings.SaveFilesPath, "*.sav");

if (choice == 0)
{
    var dbFileData = DbFileHandler.GetDbFileData(Settings.DatFileTemplatePath);

    Func<Player, bool> criteria = new(x => x.CurrentAbility >= 180);

    Console.WriteLine("-- From DB file --");
    DescribePlayer(dbFileData, criteria);

    var data = SaveGameHandler.OpenSaveGameIntoMemory(saveFiles[0]);

    Console.WriteLine("-- From save file --");
    DescribePlayer(data, criteria);
}
else
{
    var importer = new DataImporter(Console.WriteLine);

    importer.ProceedToImport(saveFiles);
}

Console.WriteLine("Process is done; Press any key to close.");
Console.ReadKey();

static void DescribePlayer(BaseFileData data, Func<Player, bool> criteria)
{
    Console.WriteLine(string.Empty);
    Console.WriteLine($"---- Player details ----");
    var p = data.Players?.FirstOrDefault(criteria);
    if (p is not null)
    {
        data.FirstNames.TryGetValue(p.FirstNameId, out var pFirstName);
        data.LastNames.TryGetValue(p.LastNameId, out var pLastName);
        data.CommonNames.TryGetValue(p.CommonNameId, out var pCommonName);

        var fullName = !string.IsNullOrWhiteSpace(pCommonName)
            ? pCommonName
            : string.Concat(pLastName, pFirstName);

        Console.WriteLine($"FullName: {fullName} - DateOfBirth: {p.DateOfBirth}");
        Console.WriteLine($"CurrentAbility: {p.CurrentAbility} - PotentialAbility: {p.PotentialAbility}");
        Console.WriteLine($"RightFoot: {p.RightFoot} - LeftFoot: {p.LeftFoot}");
        Console.WriteLine($"CurrentReputation: {p.CurrentReputation} - WorldReputation: {p.WorldReputation} - CurrentReputation: {p.CurrentReputation}");
        Console.WriteLine($"InternationalCaps: {p.InternationalCaps} - InternationalGoals: {p.InternationalGoals}");
        Console.WriteLine($"Value: {p.Value} - ContractEndDate: {p.Contract?.ContractEndDate}");
        Console.WriteLine($"Squad status: {p.Contract?.SquadStatus} - Transfer status: {p.Contract?.TransferStatus}");
        Console.WriteLine($"LeftSide: {p.LeftSide} - CentreSide: {p.CentreSide} - RightSide: {p.RightSide}");
        Console.WriteLine($"GoalKeeperPos: {p.GoalKeeperPos} - SweeperPos: {p.SweeperPos} - DefenderPos: {p.DefenderPos}");
        Console.WriteLine($"DefensiveMidfielderPos: {p.DefensiveMidfielderPos} - MidfielderPos: {p.MidfielderPos} - AttackingMidfielderPos: {p.AttackingMidfielderPos}");
        Console.WriteLine($"StrikerPos: {p.StrikerPos} - FreeRolePos: {p.FreeRolePos} - WingBackPos: {p.WingBackPos}");

        DescribeClub(data, p.ClubId, "player");
        DescribeNation(data, p.NationId, "player");
        DescribeNation(data, p.SecondaryNationId, "player");
    }
    else
    {
        Console.WriteLine("No player found!");
    }
}

static void DescribeClub(BaseFileData data, int clubId, string objectTypeFrom)
{
    Console.WriteLine(string.Empty);
    Console.WriteLine($"---- Club (from {objectTypeFrom}) details ----");
    data.Clubs.TryGetValue(clubId, out var club);
    if (club is not null)
    {
        Console.WriteLine($"Name: {club.Name} - LongName: {club.LongName}");
        Console.WriteLine($"Reputation: {club.Reputation}");
        Console.WriteLine($"Bank: {club.Bank} - Facilities: {club.Facilities}");

        DescribeClubCompetition(data, club.DivisionId, "club");
        DescribeNation(data, club.NationId, "club");
    }
    else if (clubId >= 0)
    {
        Console.WriteLine($"No club with id {clubId} found!");
    }
    else
    {
        Console.WriteLine($"Club is not set on the {objectTypeFrom}.");
    }
}

static void DescribeClubCompetition(BaseFileData data, int clubCompetitionId, string objectTypeFrom)
{
    Console.WriteLine(string.Empty);
    Console.WriteLine($"---- Club competition (from {objectTypeFrom}) details ----");
    data.ClubCompetitions.TryGetValue(clubCompetitionId, out var competition);
    if (competition is not null)
    {
        Console.WriteLine($"Name: {competition.Name} - Acronym: {competition.Acronym}");
        Console.WriteLine($"LongName: {competition.LongName} - Reputation: {competition.Reputation}");

        DescribeNation(data, competition.NationId, "club competition");
    }
    else if (clubCompetitionId >= 0)
    {
        Console.WriteLine($"No competition with id {clubCompetitionId} found!");
    }
    else
    {
        Console.WriteLine($"Competition is not set on the {objectTypeFrom}.");
    }
}

static void DescribeNation(BaseFileData data, int nationId, string objectTypeFrom)
{
    Console.WriteLine(string.Empty);
    Console.WriteLine($"---- Nation (from {objectTypeFrom}) details ----");
    data.Nations.TryGetValue(nationId, out var nation);
    if (nation is not null)
    {
        Console.WriteLine($"Name: {nation.Name} - Acronym: {nation.Acronym}");
        Console.WriteLine($"Reputation: {nation.Reputation} - LeagueStandard: {nation.LeagueStandard}");
        Console.WriteLine($"IsEu: {nation.IsEu}");

        DescribeConfederation(data, nation.ConfederationId, "nation");
    }
    else if (nationId >= 0)
    {
        Console.WriteLine($"No nation with id {nationId} found!");
    }
    else
    {
        Console.WriteLine($"Nation is not set on the {objectTypeFrom}.");
    }
}

static void DescribeConfederation(BaseFileData data, int confederationId, string objectTypeFrom)
{
    Console.WriteLine(string.Empty);
    Console.WriteLine($"---- Confederation (from {objectTypeFrom}) details ----");
    data.Confederations.TryGetValue(confederationId, out var confederation);
    if (confederation is not null)
    {
        Console.WriteLine($"Name: {confederation.Name} - Acronym: {confederation.Acronym}");
        Console.WriteLine($"ContinentName: {confederation.ContinentName} - Strength: {confederation.Strength}");
    }
    else if (confederationId >= 0)
    {
        Console.WriteLine($"No confederation with id {confederationId} found!");
    }
    else
    {
        Console.WriteLine($"Confederation is not set on the {objectTypeFrom}.");
    }
}