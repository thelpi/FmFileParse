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
        foreach (var row in p.Describe(data))
        {
            Console.WriteLine(row);
        }
    }
    else
    {
        Console.WriteLine("No player found!");
    }
}