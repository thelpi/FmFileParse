using System.Data;
using FmFileParse.Models;
using FmFileParse.Models.Internal;
using FmFileParse.SaveImport;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class DataImporter(Action<string> reportProgress)
{
    private static readonly string[] SqlColumns = [.. Settings.UnmergedOnlyColumns, .. Settings.CommonSqlColumns];

    // order is important (foreign keys)
    private static readonly string[] ResetIncrementTables =
    [
        "players", "clubs", "competitions", "countries", "confederations"
    ];

    private static readonly string[] OrderedCsvColumns =
    [
        "name", "nation", "club", "position", "ability", "potential_ability", "age", "value", "scout_rating",
        "acceleration", "adaptability", "aggression", "agility", "ambition", "anticipation", "balance", "bravery", "caps",
        "club_reputation", "consistency", "contract_expiration", "contract_type", "corners", "creativity", "crossing",
        "current_reputation", "date_of_birth", "decisions", "determination", "dirtiness", "dribbling", "finishing", "flair",
        "handling", "heading", "home_reputation", "important_matches", "influence", "injury_proneness", "international_goals",
        "jumping", "left_foot", "long_shots", "loyalty", "manager_job_rel", "marking", "min_fee_rel", "natural_fitness",
        "non_play_rel", "non_promotion_rel", "off_the_ball", "one_on_ones", "pace", "passing", "penalties", "positioning",
        "pressure", "professionalism", "reflexes", "relegation_rel", "right_foot", "set_pieces", "sportsmanship", "squad_status",
        "stamina", "strength", "tackling", "teamwork", "technique", "temperament", "throw_ins", "transfer_status", "versatility",
        "wage", "work_rate", "world_reputation"
    ];

    private static readonly string[] CsvRowsSeparators = ["\r\n", "\r", "\n"];

    private const char CsvColumnsSeparator = ';';

    private readonly Func<MySqlConnection> _getConnection =
        () => new MySqlConnection(Settings.ConnString);

    private readonly Dictionary<string, SaveGameData> _loadedSaveData = [];
    private readonly Action<string> _reportProgress = reportProgress;

    public void ProceedToImport(
        string[] saveFilePaths,
        string[] extractFilePaths)
    {
        if (saveFilePaths.Length != extractFilePaths.Length)
        {
            _reportProgress("Path lists should have the same cardinal; process interrupted.");
            return;
        }

        ClearAllData();
        var confederations = ImportConfederations(saveFilePaths);
        var countries = ImportCountries(saveFilePaths, confederations);
        var competitions = ImportCompetitions(saveFilePaths, countries);
        var clubs = ImportClubs(saveFilePaths, countries, competitions);
        ImportPlayers(saveFilePaths, extractFilePaths, countries, clubs);
    }

    private void ClearAllData()
    {
        _reportProgress("Cleaning previous data starts...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM unmerged_players";
        command.ExecuteNonQuery();

        foreach (var table in ResetIncrementTables)
        {
            command.CommandText = $"DELETE FROM {table}";
            command.ExecuteNonQuery();

            command.CommandText = $"ALTER TABLE {table} AUTO_INCREMENT = 1";
            command.ExecuteNonQuery();
        }
    }

    private SaveGameData GetSaveGameDataFromCache(
        string saveFilePath)
    {
        if (_loadedSaveData.TryGetValue(saveFilePath, out var value))
        {
            return value;
        }

        var data = SaveGameHandler.OpenSaveGameIntoMemory(saveFilePath);
        _loadedSaveData.Add(saveFilePath, data);
        return data;
    }

    private List<SaveIdMapper> ImportConfederations(
        string[] saveFilePaths)
    {
        return ImportData(x => x.Confederations,
            saveFilePaths,
            "confederations",
            new (string, DbType, Func<Confederation, int, object>)[]
            {
                ("name", DbType.String, (d, _) => d.Name),
                ("acronym", DbType.String, (d, _) => d.Acronym),
                ("continent_name", DbType.String, (d, _) => d.ContinentName),
            },
            (d, _) => d.Name);
    }

    private List<SaveIdMapper> ImportCountries(
        string[] saveFilePaths,
        List<SaveIdMapper> confederationsMapping)
    {
        var countries = ImportData(x => x.Nations,
            saveFilePaths,
            "countries",
            new (string, DbType, Func<Country, int, object>)[]
            {
                ("name", DbType.String, (d, _) => d.Name),
                ("is_eu", DbType.Boolean, (d, _) => d.IsEu == 2),
                ("reputation", DbType.Int32, (d, _) => d.Reputation),
                ("league_standard", DbType.Int32, (d, _) => d.LeagueStandard),
                ("acronym", DbType.String, (d, _) => d.Acronym),
                ("confederation_id", DbType.Int32, (d, iFile) => GetMapDbIdObject(confederationsMapping, iFile, d.ConfederationId)),
            },
            (d, iFile) => string.Concat(d.Name, ";", GetMapDbId(confederationsMapping, iFile, d.ConfederationId)));

        return countries;
    }

    private List<SaveIdMapper> ImportCompetitions(
        string[] saveFilePaths,
        List<SaveIdMapper> countriesMapping)
    {
        return ImportData(x => x.ClubComps,
            saveFilePaths,
            "competitions",
            new (string, DbType, Func<ClubComp, int, object>)[]
            {
                ("name", DbType.String, (d, iFile) => d.Name),
                ("long_name", DbType.String, (d, iFile) => d.LongName),
                ("acronym", DbType.String, (d, iFile) => d.Abbreviation),
                ("country_id", DbType.Int32, (d, iFile) => GetMapDbIdObject(countriesMapping, iFile, d.NationId)),
            },
            (d, iFile) => string.Concat(d.LongName, ";", GetMapDbId(countriesMapping, iFile, d.NationId)));
    }

    private List<SaveIdMapper> ImportClubs(
        string[] saveFilePaths,
        List<SaveIdMapper> countriesMapping,
        List<SaveIdMapper> competitionsMapping)
    {
        return ImportData(x => x.Clubs,
            saveFilePaths,
            "clubs",
            new (string, DbType, Func<Club, int, object>)[]
            {
                ("name", DbType.String, (d, iFile) => d.Name),
                ("long_name", DbType.String, (d, iFile) => d.LongName),
                ("country_id", DbType.Int32, (d, iFile) => GetMapDbIdObject(countriesMapping, iFile, d.NationId)),
                ("reputation", DbType.Int32, (d, iFile) => d.Reputation),
                ("division_id", DbType.Int32, (d, iFile) => GetMapDbIdObject(competitionsMapping, iFile, d.DivisionId)),
            },
            (d, iFile) => string.Concat(d.LongName, ";", GetMapDbId(countriesMapping, iFile, d.NationId), ";", GetMapDbId(competitionsMapping, iFile, d.DivisionId)));
    }

    private void ImportPlayers(
        string[] saveFilePaths,
        string[] extractFilePaths,
        List<SaveIdMapper> countriesMapping,
        List<SaveIdMapper> clubsMapping)
    {
        _reportProgress("Players importation starts...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = SqlColumns.GetInsertQuery("unmerged_players");

        foreach (var c in SqlColumns)
        {
            command.SetParameter(c, Settings.GetDbType(c));
        }

        command.Prepare();

        var iFile = 0;
        foreach (var saveFilePath in saveFilePaths)
        {
            var fileName = Path.GetFileName(saveFilePath);

            var data = GetSaveGameDataFromCache(saveFilePath);

            var csvData = GetDataFromCsvFile(extractFilePaths[iFile]);

            foreach (var player in data.Players)
            {
                var firstName = GetCleanDbName(player.FirstNameId, data.FirstNames);
                var lastName = GetCleanDbName(player.SecondNameId, data.Surnames);
                var commmonName = GetCleanDbName(player.CommonNameId, data.CommonNames);

                string[] keyParts =
                [
                    (commmonName == DBNull.Value ? $"{lastName}, {firstName}" : $"{commmonName}").Trim(),
                    player.WorldReputation.ToString(),
                    player.Reputation.ToString(),
                    player.DomesticReputation.ToString(),
                    player.CurrentAbility.ToString(),
                    player.PotentialAbility.ToString()
                ];

                if (!csvData.TryGetValue(string.Join(CsvColumnsSeparator, keyParts), out var csvPlayer))
                {
                    _reportProgress($"Player '{keyParts[0]}' has no CSV counterpart for file {fileName}.");
                    continue;
                }

                command.Parameters["@id"].Value = player.Id;
                command.Parameters["@filename"].Value = fileName;
                command.Parameters["@first_name"].Value = firstName;
                command.Parameters["@last_name"].Value = lastName;
                command.Parameters["@common_name"].Value = commmonName;
                command.Parameters["@date_of_birth"].Value = player.DOB;
                command.Parameters["@country_id"].Value = GetMapDbIdObject(countriesMapping, iFile, player.NationId);
                command.Parameters["@secondary_country_id"].Value = GetMapDbIdObject(countriesMapping, iFile, player.SecondaryNationId);
                command.Parameters["@caps"].Value = player.InternationalCaps;
                command.Parameters["@international_goals"].Value = player.InternationalGoals;
                command.Parameters["@right_foot"].Value = player.RightFoot;
                command.Parameters["@left_foot"].Value = player.LeftFoot;
                command.Parameters["@ability"].Value = player.CurrentAbility;
                command.Parameters["@potential_ability"].Value = player.PotentialAbility;
                command.Parameters["@home_reputation"].Value = player.DomesticReputation;
                command.Parameters["@current_reputation"].Value = player.Reputation;
                command.Parameters["@world_reputation"].Value = player.WorldReputation;
                command.Parameters["@club_id"].Value = GetMapDbIdObject(clubsMapping, iFile, player.ClubId);
                command.Parameters["@value"].Value = player.Value;
                command.Parameters["@contract_expiration"].Value = player.Contract?.ContractEndDate ?? (object)DBNull.Value;
                command.Parameters["@wage"].Value = player.Wage;
                command.Parameters["@manager_job_rel"].Value = player.Contract?.ManagerReleaseClause == true
                    ? player.Contract.ReleaseClauseValue
                    : 0;
                command.Parameters["@min_fee_rel"].Value = player.Contract?.MinimumFeeReleaseClause == true
                    ? player.Contract.ReleaseClauseValue
                    : 0;
                command.Parameters["@non_play_rel"].Value = player.Contract?.NonPlayingReleaseClause == true
                    ? player.Contract.ReleaseClauseValue
                    : 0;
                command.Parameters["@non_promotion_rel"].Value = player.Contract?.NonPromotionReleaseClause == true
                    ? player.Contract.ReleaseClauseValue
                    : 0;
                command.Parameters["@relegation_rel"].Value = player.Contract?.RelegationReleaseClause == true
                    ? player.Contract.ReleaseClauseValue
                    : 0;
                command.Parameters["@pos_goalkeeper"].Value = player.GK;
                command.Parameters["@pos_sweeper"].Value = player.SW;
                command.Parameters["@pos_defender"].Value = player.DF;
                command.Parameters["@pos_defensive_midfielder"].Value = player.DM;
                command.Parameters["@pos_midfielder"].Value = player.MF;
                command.Parameters["@pos_attacking_midfielder"].Value = player.AM;
                command.Parameters["@pos_forward"].Value = player.ST;
                command.Parameters["@pos_wingback"].Value = player.WingBack;
                command.Parameters["@pos_free_role"].Value = player.FreeRole;
                command.Parameters["@side_left"].Value = player.Left;
                command.Parameters["@side_right"].Value = player.Right;
                command.Parameters["@side_center"].Value = player.Centre;
                // TODO
                command.Parameters["@squad_status"].Value = DBNull.Value;
                command.Parameters["@transfer_status"].Value = DBNull.Value;

                // from extract file

                foreach (var attributeName in Settings.AttributeColumns)
                {
                    command.Parameters[$"@{attributeName}"].Value = csvPlayer[OrderedCsvColumns.IndexOf(attributeName)];
                }

                command.ExecuteNonQuery();

                _reportProgress($"Player '{keyParts[0]}' has been created for file {fileName}.");
            }
            iFile++;
        }
    }

    private List<SaveIdMapper> ImportData<T>(
        Func<SaveGameData, Dictionary<int, T>> sourceDataGet,
        string[] saveFilePaths,
        string tableName,
        (string pName, DbType pType, Func<T, int, object> pValue)[] parameters,
        Func<T, int, string> buildKey)
        where T : BaseData
    {
        _reportProgress($"'{tableName}' importation starts...");

        var mapping = new List<SaveIdMapper>(1000);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = parameters.Select(x => x.pName).GetInsertQuery(tableName);
        foreach (var (pName, pType, _) in parameters)
        {
            command.SetParameter(pName, pType);
        }
        command.Prepare();

        var iFile = 0;
        foreach (var saveFilePath in saveFilePaths)
        {
            var data = GetSaveGameDataFromCache(saveFilePath);

            var sourceData = sourceDataGet(data);

            foreach (var key in sourceData.Keys)
            {
                var functionnalKey = buildKey(sourceData[key], iFile);

                var match = mapping.FirstOrDefault(x => x.Key.Equals(functionnalKey, StringComparison.InvariantCultureIgnoreCase));
                if (match.Equals(default(SaveIdMapper)))
                {
                    foreach (var (pName, _, pValue) in parameters)
                    {
                        command.Parameters[$"@{pName}"].Value = pValue(sourceData[key], iFile);
                    }
                    command.ExecuteNonQuery();

                    mapping.Add(new SaveIdMapper
                    {
                        DbId = (int)command.LastInsertedId,
                        Key = functionnalKey,
                        SavesId = new Dictionary<int, List<int>>
                        {
                            { iFile, [ sourceData[key].Id ] }
                        }
                    });

                    _reportProgress($"'{functionnalKey}' has been created in '{tableName}'.");
                }
                else
                {
                    if (match.SavesId.TryGetValue(iFile, out var value))
                    {
                        value.Add(sourceData[key].Id);
                    }
                    else
                    {
                        match.SavesId.Add(iFile, [ sourceData[key].Id ]);
                    }
                }
            }

            iFile++;
        }

        return mapping;
    }

    private static object GetMapDbIdObject(List<SaveIdMapper> mapping, int fileIndex, int saveId)
    {
        var dbId = GetMapDbId(mapping, fileIndex, saveId);
        return dbId == -1 ? DBNull.Value : dbId;
    }

    private static int GetMapDbId(List<SaveIdMapper> mapping, int fileIndex, int saveId)
        => saveId < 0 ? -1 :mapping.First(x => x.SavesId.ContainsKey(fileIndex) && x.SavesId[fileIndex].Contains(saveId)).DbId;

    private static Dictionary<string, string[]> GetDataFromCsvFile(string path)
    {
        using var csvReader = new StreamReader(path, Settings.DefaultEncoding);
        var csvContent = csvReader.ReadToEnd();
        var rows = csvContent.Split(CsvRowsSeparators, StringSplitOptions.RemoveEmptyEntries);

        var playerKeys = new Dictionary<string, string[]>(rows.Length);

        // note: skips header row
        foreach (var row in rows.Skip(1))
        {
            var columns = row.Split(CsvColumnsSeparator);
            string[] keyParts =
            [
                columns[OrderedCsvColumns.IndexOf("name")].Trim(),
                columns[OrderedCsvColumns.IndexOf("world_reputation")],
                columns[OrderedCsvColumns.IndexOf("home_reputation")],
                columns[OrderedCsvColumns.IndexOf("current_reputation")],
                columns[OrderedCsvColumns.IndexOf("ability")],
                columns[OrderedCsvColumns.IndexOf("potential_ability")]
            ];
            playerKeys.Add(string.Join(CsvColumnsSeparator, keyParts), columns);
        }
        return playerKeys;
    }

    private static object GetCleanDbName(int nameId, Dictionary<int, string> names)
    {
        return names.TryGetValue(nameId, out var localName)
            && !string.IsNullOrWhiteSpace(localName)
            ? localName.Trim().Split(CsvRowsSeparators, StringSplitOptions.RemoveEmptyEntries).Last().Trim()
            : DBNull.Value;
    }

    private readonly struct SaveIdMapper
    {
        public string Key { get; init; }

        public int DbId { get; init; }

        // the value of the dictionary SHOULD be a single value
        // but the club "Torpedo Moscou" is duplicated everytime
        public Dictionary<int, List<int>> SavesId { get; init; }
    }
}
