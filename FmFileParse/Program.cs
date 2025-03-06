// See https://aka.ms/new-console-template for more information

using FmFileParse;

Console.WriteLine("1 - data importation");
Console.WriteLine("2 - players merge");
Console.WriteLine("3 - data importation the players merge");
Console.WriteLine("Other - exit");

var rawChoice = Console.ReadLine();

if (!int.TryParse(rawChoice, out var choice) || (choice != 1 && choice != 2 && choice != 3))
{
    Console.WriteLine("No action to do; Press any key to close.");
    Console.ReadKey();
    return;
}

var saveFiles = Directory.GetFiles(Settings.SaveFilesPath, $"*.{Settings.SaveFileExtension}");

if (choice == 1 || choice == 3)
{
    var csvFiles = Directory.GetFiles(Settings.CsvFilesPath, $"*.{Settings.CsvFileExtension}");

    Console.WriteLine("Reimport countries? y/n");
    rawChoice = Console.ReadLine()?.ToLowerInvariant();
    var reimportCountries = rawChoice == "y";

    var importer = new DataImporter();

    importer.ClearAllData(reimportCountries);
    if (reimportCountries)
    {
        importer.ImportCountries(saveFiles[0]);
    }
    importer.ImportCompetitions(saveFiles[0]);
    importer.ImportClubs(saveFiles[0]);
    var notCreatedPlayers = importer.ImportPlayers(saveFiles, csvFiles, x =>
        Console.WriteLine($"Player {x} created."));

    if (notCreatedPlayers.Count > 0)
    {
        Console.WriteLine($"{notCreatedPlayers.Count} players has not been created:");
        foreach (var p in notCreatedPlayers)
        {
            Console.WriteLine(p);
        }
    }
}

if (choice == 2 || choice == 3)
{
    var merger = new PlayersMerger(saveFiles.Length, x =>
        Console.WriteLine(x.Item2 ? $"[Created] {x.Item1}" : $"[Ignored] {x.Item1}"));

    merger.ProceedToMerge(true);
}

Console.WriteLine("Process is done; Press any key to close.");
Console.ReadKey();