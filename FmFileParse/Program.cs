// See https://aka.ms/new-console-template for more information

using FmFileParse;

// TODO parameter
const string ConnString = "Server=localhost;Database=cm_save_explorer;Uid=root;Pwd=;";

Console.WriteLine("1 - data importation");
Console.WriteLine("2 - players merge");
Console.WriteLine("Other - exit");
var rawChoice = Console.ReadLine();

if (!int.TryParse(rawChoice, out var choice) || (choice != 1 && choice != 2))
{
    Environment.Exit(0);
}

if (choice == 1)
{
    // TODO parameters
    var saveFiles = Directory.GetFiles("S:\\Share_VM\\saves\\test", "*.sav");
    var csvFiles = Directory.GetFiles("S:\\Share_VM\\extract", "*.csv");

    Console.WriteLine("Reimport countries? y/n");
    rawChoice = Console.ReadLine()?.ToLowerInvariant();
    var reimportCountries = rawChoice == "y";

    var importer = new DataImporter(ConnString);

    importer.ClearAllData(reimportCountries);
    if (reimportCountries)
    {
        importer.ImportCountries(saveFiles[0]);
    }
    importer.ImportCompetitions(saveFiles[0]);
    importer.ImportClubs(saveFiles[0]);
    importer.ImportPlayers(saveFiles, csvFiles);
}
else
{
    var merger = new PlayersMerger(ConnString, 12, x =>
        Console.WriteLine(x.Item2 ? $"[Created] {x.Item1}" : $"[Ignored] {x.Item1}"));

    merger.ProceedToMerge(true);
}

Console.WriteLine("Process is done; Press any key to close.");
Console.ReadKey();