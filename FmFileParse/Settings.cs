﻿namespace FmFileParse;

internal static class Settings
{
    public const string ConnString = "Server=localhost;Database=cm_save_explorer;Uid=root;Pwd=;";

    public const string SaveFilesPath = "S:\\Share_VM\\saves\\test";

    public const string DatFileTemplatePath = "./SourceFiles/{0}.dat";

    public const decimal MinValueOccurenceRate = 2 / 3M;

    public const decimal MinPlayerOccurencesRate = 1 / 3M;

    public const bool InsertStatistics = false;
}
