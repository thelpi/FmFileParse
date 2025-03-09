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