// See https://aka.ms/new-console-template for more information

using FmFileParse;
using FmFileParse.SaveImport;

Console.WriteLine("1 - data importation");
Console.WriteLine("2 - players merge");
Console.WriteLine("3 - data importation then players merge");
Console.WriteLine("4 - test the save file reader");
Console.WriteLine("Other - exit");

var rawChoice = Console.ReadLine();

if (!int.TryParse(rawChoice, out var choice) || (choice != 1 && choice != 2 && choice != 3 && choice != 4))
{
    Console.WriteLine("No action to do; Press any key to close.");
    Console.ReadKey();
    return;
}

var saveFiles = Directory.GetFiles(Settings.SaveFilesPath, $"*.{Settings.SaveFileExtension}");

if (choice == 4)
{
    var data = SaveGameHandler.OpenSaveGameIntoMemory(saveFiles[0]);

    var p = data.Players?.FirstOrDefault(x => x?.CurrentAbility > 180);
    if (p is not null)
    {
        data.FirstNames.TryGetValue(p.FirstNameId, out var pFirstName);
        data.Surnames.TryGetValue(p.SecondNameId, out var pLastName);
        data.CommonNames.TryGetValue(p.CommonNameId, out var pCommonName);

        var fullName = !string.IsNullOrWhiteSpace(pCommonName) ? pCommonName : string.Concat(pLastName, pFirstName);
        Console.WriteLine($"Name: {fullName} - Ability: {p.CurrentAbility} - WorldRep: {p.WorldReputation} - DateOfBirth: {p.DOB}");
        Console.WriteLine($"Caps: {p.InternationalCaps} - Value: {p.Value} - Adaptability: {p.Adaptability}");
        Console.WriteLine($"StrykerPos: {p.ST} - Flair: {p.Flair} - Heading: {p.Heading}");

        data.Clubs.TryGetValue(p.ClubId, out var pClub);
        if (pClub is not null)
        {
            Console.WriteLine($"Club: {pClub.LongName} - Reputation: {pClub.Reputation} - CountryId: {pClub.NationId}");
            data.ClubComps.TryGetValue(pClub.DivisionId, out var cDivision);
            if (cDivision is not null)
            {
                Console.WriteLine($"Division: {cDivision.Name} - CountryId: {cDivision.NationId}");
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
        data.Nations.TryGetValue(p.NationId, out var pCountry);
        if (pCountry is null)
        {
            Console.WriteLine($"No country with id {p.NationId} found!");
        }
        else
        {
            Console.WriteLine($"Country: {pCountry.Name} - Reputation: {pCountry.Reputation} - IsEu: {pCountry.IsEu == 2}");
            data.Confederations.TryGetValue(pCountry.ConfederationId, out var cConfederation);
            if (cConfederation is not null)
            {
                Console.WriteLine($"Confederation: {cConfederation.Name} - Acronym: {cConfederation.Acronym}");
            }
            else if (pCountry.ConfederationId >= 0)
            {
                Console.WriteLine($"No confederation with id {pCountry.ConfederationId} found!");
            }
            else
            {
                Console.WriteLine("Confederation is not set on the country.");
            }
        }
    }
    else
    {
        Console.WriteLine("No player found!");
    }
}

if (choice == 1 || choice == 3)
{
    var csvFiles = Directory.GetFiles(Settings.CsvFilesPath, $"*.{Settings.CsvFileExtension}");

    var importer = new DataImporter(Console.WriteLine);

    importer.ProceedToImport(saveFiles, csvFiles);
}

if (choice == 2 || choice == 3)
{
    var merger = new PlayersMerger(saveFiles.Length, Console.WriteLine);

    merger.ProceedToMerge();
}

Console.WriteLine("Process is done; Press any key to close.");
Console.ReadKey();