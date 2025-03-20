using FmFileParse;
using FmFileParse.Models;
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
    var data = SaveGameHandler.OpenSaveGameIntoMemory(saveFiles[0]);

    DisplayPlayerInformation(data);
}
else
{
    var importer = new DataImporter(Console.WriteLine);

    importer.ProceedToImport(saveFiles);
}

Console.WriteLine("Process is done; Press any key to close.");
Console.ReadKey();

static void DisplayPlayerInformation(FmFileParse.Models.Internal.SaveGameData data)
{
    var p = data.Players?.FirstOrDefault(x => x?.CurrentAbility > 180);
    if (p is not null)
    {
        data.FirstNames.TryGetValue(p.FirstNameId, out var pFirstName);
        data.LastNames.TryGetValue(p.LastNameId, out var pLastName);
        data.CommonNames.TryGetValue(p.CommonNameId, out var pCommonName);

        var fullName = !string.IsNullOrWhiteSpace(pCommonName) ? pCommonName : string.Concat(pLastName, pFirstName);
        Console.WriteLine($"Name: {fullName} - Ability: {p.CurrentAbility} - WorldRep: {p.WorldReputation} - DateOfBirth: {p.DateOfBirth}");
        Console.WriteLine($"Caps: {p.InternationalCaps} - Value: {p.Value} - Adaptability: {p.Adaptability}");
        Console.WriteLine($"DefenderPos: {p.DefenderPos} - Flair: {p.Flair} - Heading: {p.Heading}");

        data.Clubs.TryGetValue(p.ClubId, out var pClub);
        if (pClub is not null)
        {
            Console.WriteLine($"Club: {pClub.LongName} - Reputation: {pClub.Reputation} - NationId: {pClub.NationId}");
            data.ClubCompetitions.TryGetValue(pClub.DivisionId, out var cDivision);
            if (cDivision is not null)
            {
                Console.WriteLine($"Division: {cDivision.Name} - NationId: {cDivision.NationId}");
            }
            else if (pClub.DivisionId >= 0)
            {
                Console.WriteLine($"No division with id {pClub.DivisionId} found!");
            }
            else
            {
                Console.WriteLine("Division is not set on the club.");
            }
        }

        else if (p.ClubId >= 0)
        {
            Console.WriteLine($"No club with id {p.ClubId} found!");
        }
        data.Nations.TryGetValue(p.NationId, out var pNation);
        if (pNation is null)
        {
            Console.WriteLine($"No nation with id {p.NationId} found!");
        }
        else
        {
            Console.WriteLine($"Nation: {pNation.Name} - Reputation: {pNation.Reputation} - IsEu: {pNation.IsEu == 2}");
            data.Confederations.TryGetValue(pNation.ConfederationId, out var cConfederation);
            if (cConfederation is not null)
            {
                Console.WriteLine($"Confederation: {cConfederation.Name} - Acronym: {cConfederation.Acronym}");
            }
            else if (pNation.ConfederationId >= 0)
            {
                Console.WriteLine($"No confederation with id {pNation.ConfederationId} found!");
            }
            else
            {
                Console.WriteLine("Confederation is not set on the nation.");
            }
        }

        // standard
        Console.WriteLine($"{nameof(Player.Aggression)}: {p.Aggression}");
        // reversed
        Console.WriteLine($"{nameof(Player.InjuryProneness)}: {p.InjuryProneness}");
        // intrinsic global
        Console.WriteLine($"{nameof(Player.Anticipation)}: {p.Anticipation}");
        // intrinsic goalkeeper
        Console.WriteLine($"{nameof(Player.OneOnOnes)}: {p.OneOnOnes}");
        // intrinsic field player
        Console.WriteLine($"{nameof(Player.OffTheBall)}: {p.OffTheBall}");
    }
    else
    {
        Console.WriteLine("No player found!");
    }
}