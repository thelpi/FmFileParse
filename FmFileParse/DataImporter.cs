using System.Data;
using System.Text;
using FmFileParse.DataClasses;
using FmFileParse.Models;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class DataImporter(Action<string> reportProgress)
{
    private static readonly string[] SqlColumns = [.. Settings.UnmergedOnlyColumns, .. Settings.CommonSqlColumns];

    // order is important (foreign keys)
    private static readonly string[] ResetIncrementTables =
    [
        "players", "clubs", "competitions", "countries"
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

    private static readonly Encoding CsvFileEncoding = Encoding.Latin1;

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
        var countries = ImportCountries(saveFilePaths);
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

    private List<SaveIdMapper> ImportCountries(
        string[] saveFilePaths)
    {
        _reportProgress("Countries importation starts...");

        var countries = new List<SaveIdMapper>(250);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        // TODO: confederation, is_eu...
        command.CommandText = "INSERT INTO countries (name, is_eu, confederation_id) " +
            "VALUES (@name, 0, 1)";
        command.SetParameter("name", DbType.String);
        command.Prepare();

        var iFile = 0;
        foreach (var saveFilePath in saveFilePaths)
        {
            var data = GetSaveGameDataFromCache(saveFilePath);

            foreach (var key in data.Nations.Keys)
            {
                if (!TryAddSaveIdToMap(data.Nations, countries, iFile, data.Nations[key].Name, key))
                {
                    command.Parameters["@name"].Value = data.Nations[key].Name;
                    command.ExecuteNonQuery();

                    countries.Add(new SaveIdMapper
                    {
                        DbId = (int)command.LastInsertedId,
                        Key = data.Nations[key].Name,
                        SavesId = new Dictionary<int, int>
                        {
                            { iFile, data.Nations[key].Id }
                        }
                    });

                    _reportProgress($"Country '{data.Nations[key].Name}' has been created.");
                }
            }
            iFile++;
        }

        // TODO ...then remove this section
        command.CommandText = "UPDATE countries SET is_eu = " +
            "(SELECT c.is_eu FROM countries_backup AS c WHERE c.name = countries.name)";
        command.ExecuteNonQuery();

        command.CommandText = "UPDATE countries SET confederation_id = " +
            "(SELECT c.confederation_id FROM countries_backup AS c WHERE c.name = countries.name)";
        command.ExecuteNonQuery();

        return countries;
    }

    private List<SaveIdMapper> ImportCompetitions(
        string[] saveFilePaths,
        List<SaveIdMapper> countriesMapping)
    {
        _reportProgress("Competitions importation starts...");

        var competitions = new List<SaveIdMapper>(500);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO competitions (name, long_name, acronym, country_id) " +
            "VALUES (@name, @long_name, @acronym, @country_id)";
        command.SetParameter("name", DbType.String);
        command.SetParameter("long_name", DbType.String);
        command.SetParameter("acronym", DbType.String);
        command.SetParameter("country_id", DbType.Int32);
        command.Prepare();

        var iFile = 0;
        foreach (var saveFilePath in saveFilePaths)
        {
            var data = GetSaveGameDataFromCache(saveFilePath);

            foreach (var key in data.ClubComps.Keys)
            {
                var countryId = data.ClubComps[key].NationId >= 0
                    ? GetMapDbId(countriesMapping, iFile, data.ClubComps[key].NationId)
                    : -1;

                var competitionKey = string.Concat(data.ClubComps[key].LongName, ";", countryId);

                if (!TryAddSaveIdToMap(data.ClubComps, competitions, iFile, competitionKey, key))
                {
                    command.Parameters["@name"].Value = data.ClubComps[key].Name;
                    command.Parameters["@long_name"].Value = data.ClubComps[key].LongName;
                    command.Parameters["@acronym"].Value = data.ClubComps[key].Abbreviation;
                    command.Parameters["@country_id"].Value = countryId == -1 ? DBNull.Value : countryId;
                    command.ExecuteNonQuery();

                    competitions.Add(new SaveIdMapper
                    {
                        DbId = (int)command.LastInsertedId,
                        Key = competitionKey,
                        SavesId = new Dictionary<int, int>
                        {
                            { iFile, data.ClubComps[key].Id }
                        }
                    });

                    _reportProgress($"Competition '{data.ClubComps[key].Name}' has been created.");
                }
            }
            iFile++;
        }

        return competitions;
    }

    private List<SaveIdMapper> ImportClubs(
        string[] saveFilePaths,
        List<SaveIdMapper> countriesMapping,
        List<SaveIdMapper> competitionsMapping)
    {
        _reportProgress("Clubs importation starts...");

        var clubs = new List<SaveIdMapper>(1000);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO clubs (name, long_name, country_id, reputation, division_id) " +
            "VALUES (@name, @long_name, @country_id, @reputation, @division_id)";

        command.SetParameter("name", DbType.String);
        command.SetParameter("long_name", DbType.String);
        command.SetParameter("country_id", DbType.Int32);
        command.SetParameter("reputation", DbType.Int32);
        command.SetParameter("division_id", DbType.Int32);

        command.Prepare();

        var iFile = 0;
        foreach (var saveFilePath in saveFilePaths)
        {
            var data = GetSaveGameDataFromCache(saveFilePath);

            foreach (var key in data.Clubs.Keys)
            {
                var countryId = data.Clubs[key].NationId < 0
                    ? -1
                    : GetMapDbId(countriesMapping, iFile, data.Clubs[key].NationId);
                var divisionId = data.Clubs[key].DivisionId < 0
                    ? -1
                    : GetMapDbId(competitionsMapping, iFile, data.Clubs[key].DivisionId);
                var clubKey = string.Concat(data.Clubs[key].LongName, ";", countryId, ";", data.Clubs[key].Reputation);

                if (!TryAddSaveIdToMap(data.Clubs, clubs, iFile, clubKey, key))
                {
                    command.Parameters["@name"].Value = data.Clubs[key].Name;
                    command.Parameters["@long_name"].Value = data.Clubs[key].LongName;
                    command.Parameters["@country_id"].Value = countryId == -1 ? DBNull.Value : countryId;
                    command.Parameters["@reputation"].Value = data.Clubs[key].Reputation;
                    command.Parameters["@division_id"].Value = divisionId == -1 ? DBNull.Value : divisionId;
                    command.ExecuteNonQuery();

                    clubs.Add(new SaveIdMapper
                    {
                        DbId = (int)command.LastInsertedId,
                        Key = clubKey,
                        SavesId = new Dictionary<int, int>
                        {
                            { iFile, data.Clubs[key].Id }
                        }
                    });

                    _reportProgress($"Club '{data.Clubs[key].Name}' has been created.");
                }
            }

            iFile++;
        }

        return clubs;
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
                command.Parameters["@country_id"].Value = GetMapDbId(countriesMapping, iFile, player.NationId);
                command.Parameters["@secondary_country_id"].Value = player.SecondaryNationId >= 0
                    ? GetMapDbId(countriesMapping, iFile, player.SecondaryNationId)
                    : DBNull.Value;
                command.Parameters["@caps"].Value = player.InternationalCaps;
                command.Parameters["@international_goals"].Value = player.InternationalGoals;
                command.Parameters["@right_foot"].Value = player.RightFoot;
                command.Parameters["@left_foot"].Value = player.LeftFoot;
                command.Parameters["@ability"].Value = player.CurrentAbility;
                command.Parameters["@potential_ability"].Value = player.PotentialAbility;
                command.Parameters["@home_reputation"].Value = player.DomesticReputation;
                command.Parameters["@current_reputation"].Value = player.Reputation;
                command.Parameters["@world_reputation"].Value = player.WorldReputation;
                command.Parameters["@club_id"].Value = player.ClubId >= 0
                    ? GetMapDbId(clubsMapping, iFile, player.ClubId)
                    : DBNull.Value;
                command.Parameters["@value"].Value = player.Value;
                command.Parameters["@contract_expiration"].Value = player.Contract?.ContractEndDate ?? (object)DBNull.Value;
                command.Parameters["@wage"].Value = player.Wage;
                // TODO: check, it's probably wrong
                command.Parameters["@transfer_status"].Value = player.Contract is not null && Enum.IsDefined((TransferStatus)player.Contract.TransferStatus)
                    ? ((TransferStatus)player.Contract.TransferStatus).ToString()
                    : DBNull.Value;
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

                // from extract file

                // TODO: source version is unreliable for now
                /*command.Parameters["@squad_status"].Value = player._contract is not null && Enum.IsDefined((SquadStatus)player._contract.SquadStatus)
                    ? ((SquadStatus)player._contract.SquadStatus).ToString()
                    : DBNull.Value;*/
                var sourceValue = csvPlayer[OrderedCsvColumns.IndexOf("squad_status")];
                command.Parameters["@squad_status"].Value = string.IsNullOrWhiteSpace(sourceValue)
                    ? DBNull.Value
                    : sourceValue;

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

    private static int GetMapDbId(List<SaveIdMapper> mapping, int fileIndex, int saveId)
        => mapping.First(x => x.SavesId.ContainsKey(fileIndex) && x.SavesId[fileIndex] == saveId).DbId;

    private static Dictionary<string, string[]> GetDataFromCsvFile(string path)
    {
        using var csvReader = new StreamReader(path, CsvFileEncoding);
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

    private static bool TryAddSaveIdToMap<T>(
        Dictionary<int, T> dataDict,
        List<SaveIdMapper> mapping,
        int fileIndex,
        string dataKey,
        int dataId)
        where T : BaseData
    {
        var match = mapping.FirstOrDefault(x => x.Key.Equals(dataKey, StringComparison.InvariantCultureIgnoreCase));
        if (!match.Equals(default(SaveIdMapper)))
        {
            match.SavesId.Add(fileIndex, dataDict[dataId].Id);
            return true;
        }
        return false;
    }

    private readonly struct SaveIdMapper
    {
        public string Key { get; init; }

        public int DbId { get; init; }

        public Dictionary<int, int> SavesId { get; init; }
    }
}
