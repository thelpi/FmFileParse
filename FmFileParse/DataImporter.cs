using System.Data;
using System.Linq;
using System.Numerics;
using FmFileParse.Models;
using FmFileParse.Models.Internal;
using FmFileParse.SaveImport;
using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class DataImporter(Action<string> reportProgress)
{
    private static readonly string[] SqlColumns = [.. Settings.UnmergedOnlyColumns, .. Settings.CommonSqlColumns];

    private static readonly string[] NameNewLineSeparators = ["\r\n", "\r", "\n"];

    // order is important (foreign keys)
    private static readonly string[] ResetIncrementTables =
    [
        "players", "clubs", "club_competitions", "nations", "confederations"
    ];

    private readonly Func<MySqlConnection> _getConnection =
        () => new MySqlConnection(Settings.ConnString);

    private readonly Dictionary<string, SaveGameData> _loadedSaveData = [];
    private readonly Action<string> _reportProgress = reportProgress;

    public void ProceedToImport(string[] saveFilePaths)
    {
        ClearAllData();

        var confederations = ImportConfederations(saveFilePaths);
        SetSaveFileReferences(confederations, nameof(Confederation));

        var nations = ImportNations(saveFilePaths, confederations);
        SetSaveFileReferences(nations, nameof(Nation));

        var clubCompetitions = ImportClubCompetitions(saveFilePaths, nations);
        SetSaveFileReferences(clubCompetitions, nameof(ClubCompetition));

        var clubs = ImportClubs(saveFilePaths, nations, clubCompetitions);
        SetClubsInformation(saveFilePaths, clubs);
        SetSaveFileReferences(clubs, nameof(Club));

        ImportPlayers2(saveFilePaths, nations, clubs);
    }

    internal void UpdateStaffOnClubs(List<SaveIdMapper> players, string[] saveFilePaths)
    {
        using var wConnection = _getConnection();
        wConnection.Open();
        using var wCommand = wConnection.CreateCommand();
        wCommand.CommandText = "UPDATE clubs " +
            "SET liked_staff_1 = @liked_staff_1, liked_staff_2 = @liked_staff_2, liked_staff_3 = @liked_staff_3, " +
            "disliked_staff_1 = @disliked_staff_1, disliked_staff_2 = @disliked_staff_2, disliked_staff_3 = @disliked_staff_3 " +
            "WHERE id = @id";
        wCommand.SetParameter("liked_staff_1", DbType.Int32);
        wCommand.SetParameter("liked_staff_2", DbType.Int32);
        wCommand.SetParameter("liked_staff_3", DbType.Int32);
        wCommand.SetParameter("disliked_staff_1", DbType.Int32);
        wCommand.SetParameter("disliked_staff_2", DbType.Int32);
        wCommand.SetParameter("disliked_staff_3", DbType.Int32);
        wCommand.SetParameter("id", DbType.Int32);
        wCommand.Prepare();

        int? GetMatchDbId(int pSaveId, int fileId)
        {
            if (pSaveId < 0)
            {
                return null;
            }

            var match = players.FirstOrDefault(x => x.SaveId.Any(kvp => kvp.Key == fileId && kvp.Value == pSaveId));
            return match.Equals(default(SaveIdMapper))
                ? null
                : match.DbId;
        }

        var aggregatedData = new Dictionary<int, List<(int? ls1, int? ls2, int? ls3, int? ds1, int? ds2, int? ds3)>>(players.Count);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM save_files_references WHERE data_type = @data_type ORDER BY data_id, file_id";
        command.SetParameter("@data_type", DbType.String, nameof(Club));
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var fileId = reader.GetInt32("file_id");
            var pId = reader.GetInt32("data_id");

            var saveData = GetSaveGameDataFromCache(saveFilePaths[fileId]);
            var club = saveData.Clubs[reader.GetInt32("save_id")];
            
            var ls1 = GetMatchDbId(club.LikedStaff1, fileId);
            var ls2 = GetMatchDbId(club.LikedStaff2, fileId);
            var ls3 = GetMatchDbId(club.LikedStaff3, fileId);
            var ds1 = GetMatchDbId(club.DislikedStaff1, fileId);
            var ds2 = GetMatchDbId(club.DislikedStaff2, fileId);
            var ds3 = GetMatchDbId(club.DislikedStaff3, fileId);

            if (!aggregatedData.TryGetValue(pId, out var value))
            {
                value = new List<(int? ls1, int? ls2, int? ls3, int? ds1, int? ds2, int? ds3)>(12);
                aggregatedData.Add(pId, value);
            }

            value.Add((ls1, ls2, ls3, ds1, ds2, ds3));
        }

        foreach (var pid in aggregatedData.Keys)
        {
            var ls1 = aggregatedData[pid].GetMaxOccurence(x => x.ls1)!;
            var ls2 = aggregatedData[pid].GetMaxOccurence(x => x.ls2)!;
            var ls3 = aggregatedData[pid].GetMaxOccurence(x => x.ls3)!;
            var ds1 = aggregatedData[pid].GetMaxOccurence(x => x.ds1)!;
            var ds2 = aggregatedData[pid].GetMaxOccurence(x => x.ds2)!;
            var ds3 = aggregatedData[pid].GetMaxOccurence(x => x.ds3)!;

            if (ls1.Key.HasValue || ls2.Key.HasValue || ls3.Key.HasValue
                || ds1.Key.HasValue || ds2.Key.HasValue || ds3.Key.HasValue)
            {
                wCommand.Parameters["@liked_staff_1"].Value = (object?)ls1.Key ?? DBNull.Value;
                wCommand.Parameters["@liked_staff_2"].Value = (object?)ls2.Key ?? DBNull.Value;
                wCommand.Parameters["@liked_staff_3"].Value = (object?)ls3.Key ?? DBNull.Value;
                wCommand.Parameters["@disliked_staff_1"].Value = (object?)ds1.Key ?? DBNull.Value;
                wCommand.Parameters["@disliked_staff_2"].Value = (object?)ds2.Key ?? DBNull.Value;
                wCommand.Parameters["@disliked_staff_3"].Value = (object?)ds3.Key ?? DBNull.Value;
                wCommand.Parameters["@id"].Value = pid;
                wCommand.ExecuteNonQuery();
            }
        }
    }

    internal void SetSaveFileReferences(List<SaveIdMapper> data, string dataTypeName)
    {
        if (data.Count == 0)
        {
            return;
        }

        const int CountByLot = 100;

        var lotCount = (data.Count / CountByLot) + 1;
        for (var i = 0; i < lotCount; i++)
        {
            var sqlRowValues = new List<string>(CountByLot * 12);
            foreach (var cMap in data.Skip(i * CountByLot).Take(CountByLot))
            {
                foreach (var cMapIdKey in cMap.SaveId.Keys)
                {
                    sqlRowValues.Add($"('{MySqlHelper.EscapeString(dataTypeName)}', {cMap.DbId}, {cMapIdKey}, {cMap.SaveId[cMapIdKey]})");
                }
            }

            using var connection = _getConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"INSERT INTO save_files_references ({string.Join(", ", Settings.SaveFilesReferencesColumns)}) " +
                $"VALUES {string.Join(", ", sqlRowValues)}";
            command.ExecuteNonQuery();
        }
    }

    private void ClearAllData()
    {
        _reportProgress("Cleaning previous data starts...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM unmerged_players";
        command.ExecuteNonQuery();

        command.CommandText = "TRUNCATE TABLE players_merge_statistics";
        command.ExecuteNonQuery();

        command.CommandText = "TRUNCATE TABLE save_files_references";
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
                ("strength", DbType.Int16, (d, _) => d.Strength * 100)
            },
            (d, _) => d.Name);
    }

    private List<SaveIdMapper> ImportNations(
        string[] saveFilePaths,
        List<SaveIdMapper> confederationsMapping)
    {
        var nations = ImportData(x => x.Nations,
            saveFilePaths,
            "nations",
            new (string, DbType, Func<Nation, int, object>)[]
            {
                ("name", DbType.String, (d, _) => d.Name),
                ("is_eu", DbType.Boolean, (d, _) => d.IsEu == 2),
                ("reputation", DbType.Int32, (d, _) => d.Reputation),
                ("league_standard", DbType.Int32, (d, _) => d.LeagueStandard),
                ("acronym", DbType.String, (d, _) => d.Acronym),
                ("confederation_id", DbType.Int32, (d, iFile) => GetMapDbIdObject(confederationsMapping, iFile, d.ConfederationId)),
            },
            (d, iFile) => string.Concat(d.Name, ";", GetMapDbId(confederationsMapping, iFile, d.ConfederationId)));

        return nations;
    }

    private List<SaveIdMapper> ImportClubCompetitions(
        string[] saveFilePaths,
        List<SaveIdMapper> nationsMapping)
    {
        return ImportData(x => x.ClubCompetitions,
            saveFilePaths,
            "club_competitions",
            new (string, DbType, Func<ClubCompetition, int, object>)[]
            {
                ("name", DbType.String, (d, iFile) => d.Name),
                ("long_name", DbType.String, (d, iFile) => d.LongName),
                ("acronym", DbType.String, (d, iFile) => d.Acronym),
                ("nation_id", DbType.Int32, (d, iFile) => GetMapDbIdObject(nationsMapping, iFile, d.NationId)),
                ("reputation", DbType.Int32, (d, iFile) => d.Reputation)
            },
            (d, iFile) => string.Concat(d.LongName, ";", GetMapDbId(nationsMapping, iFile, d.NationId)));
    }

    private List<SaveIdMapper> ImportClubs(
        string[] saveFilePaths,
        List<SaveIdMapper> nationsMapping,
        List<SaveIdMapper> clubCompetitionsMapping)
    {
        return ImportData(x => x.Clubs,
            saveFilePaths,
            "clubs",
            new (string, DbType, Func<Club, int, object>)[]
            {
                ("name", DbType.String, (d, iFile) => d.Name),
                ("long_name", DbType.String, (d, iFile) => d.LongName),
                ("nation_id", DbType.Int32, (d, iFile) => GetMapDbIdObject(nationsMapping, iFile, d.NationId)),
                ("division_id", DbType.Int32, (d, iFile) => GetMapDbIdObject(clubCompetitionsMapping, iFile, d.DivisionId))
            },
            // note: the key here should be the same as the one used in 'DataFileLoaders.ManageDuplicateClubs'
            (d, iFile) => string.Concat(d.LongName, ";", GetMapDbId(nationsMapping, iFile, d.NationId), ";", GetMapDbId(clubCompetitionsMapping, iFile, d.DivisionId)));
    }

    private void ImportPlayers2(
        string[] saveFilePaths,
        List<SaveIdMapper> nationsMapping,
        List<SaveIdMapper> clubsMapping)
    {
        _reportProgress("Players importation starts...");

        SetForeignKeysCheck(false);

        var allColumns = new List<string>(Settings.CommonSqlColumns)
        {
            "occurences"
        };

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = allColumns.GetInsertQuery("players");
        foreach (var c in allColumns)
        {
            command.SetParameter(c, Settings.GetDbType(c));
        }
        command.Prepare();

        var collectedMergeInfo = new Dictionary<int, List<(string field, int occurences, MergeType mergeType)>>(520);
        var collectedDbIdMap = new List<SaveIdMapper>(10000);

        var nameKeys = new List<(string nameKey, int fileId, DateTime dob, int clubId, Player p)>(saveFilePaths.Length * 10000);
        for (var fileId = 0; fileId < saveFilePaths.Length; fileId++)
        {
            var data = GetSaveGameDataFromCache(saveFilePaths[fileId]);

            foreach (var p in data.Players)
            {
                var firstName = GetNameValue(p.FirstNameId, data.FirstNames);
                var lastName = GetNameValue(p.LastNameId, data.LastNames);
                var commmonName = GetNameValue(p.CommonNameId, data.CommonNames);
                var clubId = GetMapDbId(clubsMapping, fileId, p.ClubId);
                nameKeys.Add(($"{firstName};{lastName};{commmonName}".ToLowerInvariant(), fileId, p.DateOfBirth, clubId, p));
            }
        }

        foreach (var nameKey in nameKeys.Select(x => x.nameKey).Distinct())
        {
            var countDistinct = nameKeys.Where(x => x.nameKey == nameKey).Select(x => x.fileId).Distinct().Count();
            var countStandard = nameKeys.Where(x => x.nameKey == nameKey).Select(x => x.fileId).Count();
            if (countStandard == countDistinct)
            {
                var players = nameKeys.Where(x => x.nameKey == nameKey).Select(x => (x.p, x.fileId));
                var mapId = InsertMergedPlayer(players, command, collectedMergeInfo, saveFilePaths, nationsMapping, clubsMapping);
                if (mapId.HasValue)
                {
                    collectedDbIdMap.Add(mapId.Value);
                }
            }
            else
            {
                var withDob = nameKeys.Where(x => x.nameKey == nameKey).Select(x => (x.nameKey, x.dob)).Distinct().ToList();
                foreach (var dob in withDob)
                {
                    countDistinct = nameKeys.Where(x => (x.nameKey, x.dob) == dob).Select(x => x.fileId).Distinct().Count();
                    countStandard = nameKeys.Where(x => (x.nameKey, x.dob) == dob).Select(x => x.fileId).Count();
                    if (countStandard == countDistinct)
                    {
                        var players = nameKeys.Where(x => (x.nameKey, x.dob) == dob).Select(x => (x.p, x.fileId));
                        InsertMergedPlayer(players, command, collectedMergeInfo, saveFilePaths, nationsMapping, clubsMapping);
                    }
                    else
                    {
                        var withDobClub = nameKeys.Where(x => (x.nameKey, x.dob) == dob).Select(x => (x.nameKey, x.dob, x.clubId)).Distinct().ToList();
                        foreach (var dobClub in withDobClub)
                        {
                            countDistinct = nameKeys.Where(x => (x.nameKey, x.dob, x.clubId) == dobClub).Select(x => x.fileId).Distinct().Count();
                            countStandard = nameKeys.Where(x => (x.nameKey, x.dob, x.clubId) == dobClub).Select(x => x.fileId).Count();
                            if (countStandard == countDistinct)
                            {
                                var players = nameKeys.Where(x => (x.nameKey, x.dob, x.clubId) == dobClub).Select(x => (x.p, x.fileId));
                                InsertMergedPlayer(players, command, collectedMergeInfo, saveFilePaths, nationsMapping, clubsMapping);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                }
            }

            if (collectedMergeInfo.Count >= 500)
            {
                BulkInsertPlayerMergeStatistics(collectedMergeInfo);
            }
        }

        if (collectedMergeInfo.Count > 0)
        {
            BulkInsertPlayerMergeStatistics(collectedMergeInfo);
        }

        SetForeignKeysCheck(true);

        _reportProgress("Saves savefiles references map...");

        SetSaveFileReferences(collectedDbIdMap, nameof(Player));

        _reportProgress("Updates club's staff information...");

        UpdateStaffOnClubs(collectedDbIdMap, saveFilePaths);
    }

    private void BulkInsertPlayerMergeStatistics(
        Dictionary<int, List<(string field, int occurences, MergeType mergeType)>> collectedMergeInfo)
    {
        var informationCopy = collectedMergeInfo.ToDictionary(x => x.Key, x => x.Value);

        collectedMergeInfo.Clear();

        Task.Run(() =>
        {
            var rowsToInsert = new List<string>(informationCopy.Count * 100);
            foreach (var playerId in informationCopy.Keys)
            {
                foreach (var (field, occurences, mergeType) in informationCopy[playerId])
                {
                    rowsToInsert.Add($"({playerId}, '{MySqlHelper.EscapeString(field)}', {occurences}, '{mergeType}')");
                }
            }
            using var connection = _getConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"INSERT INTO players_merge_statistics (player_id, field, occurences, merge_type) VALUES {string.Join(", ", rowsToInsert)}";
            command.ExecuteNonQuery();
        });
    }

    private void SetForeignKeysCheck(bool enable)
    {
        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SET FOREIGN_KEY_CHECKS = {(enable ? 1 : 0)}";
        command.ExecuteNonQuery();
    }

    private SaveIdMapper? InsertMergedPlayer(
        IEnumerable<(Player, int)> players,
        MySqlCommand insertPlayerCommand,
        Dictionary<int, List<(string field, int occurences, MergeType mergeType)>> collectedMergeInfo,
        string[] saveFilePaths,
        List<SaveIdMapper> nationsMapping,
        List<SaveIdMapper> clubsMapping)
    {
        // there's not enough data across all files for the player
        if (players.Count() / (decimal)12 < Settings.MinPlayerOccurencesRate)
        {
            _reportProgress($"The player has not enough data to be merged.");
            return null;
        }

        var allFilePlayerData = new List<Dictionary<string, object>>(12);
        var collectedSaveIds = new Dictionary<int, int>();
        foreach (var (player, fileId) in players)
        {
            var data = GetSaveGameDataFromCache(saveFilePaths[fileId]);

            var singleFilePlayerData = new Dictionary<string, object>
            {
                { "first_name", GetCleanDbName(player.FirstNameId, data.FirstNames) },
                { "last_name", GetCleanDbName(player.LastNameId, data.LastNames) },
                { "common_name", GetCleanDbName(player.CommonNameId, data.CommonNames) },
                { "date_of_birth", player.DateOfBirth },
                { "nation_id", GetMapDbIdObject(nationsMapping, fileId, player.NationId) },
                { "secondary_nation_id", GetMapDbIdObject(nationsMapping, fileId, player.SecondaryNationId) },
                { "caps", player.InternationalCaps },
                { "international_goals", player.InternationalGoals },
                { "right_foot", player.RightFoot },
                { "left_foot", player.LeftFoot },
                { "ability", player.CurrentAbility },
                { "potential_ability", player.PotentialAbility },
                { "home_reputation", player.HomeReputation },
                { "current_reputation", player.CurrentReputation },
                { "world_reputation", player.WorldReputation },
                { "club_id", GetMapDbIdObject(clubsMapping, fileId, player.ClubId) },
                { "value", player.Value },
                { "contract_expiration", player.Contract?.ContractEndDate ?? (object)DBNull.Value },
                { "wage", player.Wage },
                { "manager_job_rel", player.Contract?.ManagerReleaseClause == true
                    ? player.Contract.ReleaseClauseValue
                    : 0 },
                { "min_fee_rel", player.Contract?.MinimumFeeReleaseClause == true
                    ? player.Contract.ReleaseClauseValue
                    : 0 },
                { "non_play_rel", player.Contract?.NonPlayingReleaseClause == true
                    ? player.Contract.ReleaseClauseValue
                    : 0 },
                { "non_promotion_rel", player.Contract?.NonPromotionReleaseClause == true
                    ? player.Contract.ReleaseClauseValue
                    : 0 },
                { "relegation_rel", player.Contract?.RelegationReleaseClause == true
                    ? player.Contract.ReleaseClauseValue
                    : 0 },
                { "pos_goalkeeper", player.GoalKeeperPos },
                { "pos_sweeper", player.SweeperPos },
                { "pos_defender", player.DefenderPos },
                { "pos_defensive_midfielder", player.DefensiveMidfielderPos },
                { "pos_midfielder", player.MidfielderPos },
                { "pos_attacking_midfielder", player.AttackingMidfielderPos },
                { "pos_forward", player.StrikerPos },
                { "pos_wingback", player.WingBackPos },
                { "pos_free_role", player.FreeRolePos },
                { "side_left", player.LeftSide },
                { "side_right", player.RightSide },
                { "side_center", player.CentreSide },
                { "squad_status", DBNull.Value },
                { "transfer_status", DBNull.Value },
                { "anticipation", player.Anticipation },
                { "acceleration", player.Acceleration },
                { "adaptability", player.Adaptability },
                { "aggression", player.Aggression },
                { "agility", player.Agility },
                { "ambition", player.Ambition },
                { "balance", player.Balance },
                { "bravery", player.Bravery },
                { "consistency", player.Consistency },
                { "corners", player.Corners },
                { "creativity", player.Creativity },
                { "crossing", player.Crossing },
                { "decisions", player.Decisions },
                { "determination", player.Determination },
                { "dirtiness", player.Dirtiness },
                { "dribbling", player.Dribbling },
                { "finishing", player.Finishing },
                { "flair", player.Flair },
                { "handling", player.Handling },
                { "heading", player.Heading },
                { "important_matches", player.ImportantMatches },
                { "influence", player.Influence },
                { "injury_proneness", player.InjuryProneness },
                { "jumping", player.Jumping },
                { "long_shots", player.LongShots },
                { "loyalty", player.Loyalty },
                { "marking", player.Marking },
                { "natural_fitness", player.NaturalFitness },
                { "off_the_ball", player.OffTheBall },
                { "one_on_ones", player.OneOnOnes },
                { "pace", player.Pace },
                { "passing", player.Passing },
                { "penalties", player.Penalties },
                { "positioning", player.Positioning },
                { "pressure", player.Pressure },
                { "professionalism", player.Professionalism },
                { "reflexes", player.Reflexes },
                { "set_pieces", player.FreeKicks },
                { "sportsmanship", player.Sportsmanship },
                { "stamina", player.Stamina },
                { "strength", player.Strength },
                { "tackling", player.Tackling },
                { "teamwork", player.Teamwork },
                { "technique", player.Technique },
                { "temperament", player.Temperament },
                { "throw_ins", player.ThrowIns },
                { "versatility", player.Versatility },
                { "work_rate", player.WorkRate }
            };

            allFilePlayerData.Add(singleFilePlayerData);
            collectedSaveIds.Add(fileId, player.Id);
        }

        var colsStats = new List<(string field, int distinctOccurences, MergeType mergeType)>(SqlColumns.Length);
        var colsAndVals = new Dictionary<string, object>(SqlColumns.Length);
        foreach (var col in allFilePlayerData[0].Keys)
        {
            Func<IEnumerable<object>, object>? averageFunc = null;
            if (Settings.DateColumns.Contains(col))
            {
                averageFunc = values => values.Select(Convert.ToDateTime).Average();
            }
            else if (!Settings.StringColumns.Contains(col) && !Settings.ForeignKeyColumns.Contains(col))
            {
                averageFunc = values => (int)Math.Round(values.Select(Convert.ToInt32).Average());
            }

            var (computedValue, mergeType, countValues) = CrawlColumnValuesForMerge(
                allFilePlayerData,
                allFilePlayerData.Select(_ => _[col]),
                averageFunc);

            colsAndVals.Add(col, computedValue);
            colsStats.Add((col, countValues, mergeType));
        }

        colsAndVals.Add("occurences", allFilePlayerData.Count);

        foreach (var c in colsAndVals.Keys)
        {
            insertPlayerCommand.Parameters[$"@{c}"].Value = colsAndVals[c];
        }
        insertPlayerCommand.ExecuteNonQuery();

        var dbPlayerId = (int)insertPlayerCommand.LastInsertedId;

        collectedMergeInfo.Add(dbPlayerId, colsStats);

        _reportProgress($"The player has been merged.");

        return new SaveIdMapper
        {
            DbId = dbPlayerId,
            SaveId = collectedSaveIds
        };
    }

    private static (object computedValue, MergeType mergeType, int countValues) CrawlColumnValuesForMerge(
        List<Dictionary<string, object>> allFilePlayerData,
        IEnumerable<object> allValues,
        Func<IEnumerable<object>, object>? averageFunc)
    {
        // note: there's an arbitrary choice when the two first occurences have the same count
        var groupedValues = allValues
            .GroupBy(x => x)
            .Select(x => (Value: x.Key, Count: x.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        MergeType mergeType;
        object value;
        if (groupedValues[0].Count < Settings.MinValueOccurenceRate * allFilePlayerData.Count)
        {
            if (averageFunc is not null && !groupedValues.Any(x => x.Value == DBNull.Value))
            {
                value = averageFunc(allValues);
                mergeType = MergeType.Average;
            }
            else
            {
                value = groupedValues[0].Value;
                mergeType = MergeType.ModeBelowThreshold;
            }
        }
        else
        {
            value = groupedValues[0].Value;
            mergeType = MergeType.ModeAboveThreshold;
        }

        return (value, mergeType, groupedValues.Count);
    }

    private void ImportPlayers(
        string[] saveFilePaths,
        List<SaveIdMapper> nationsMapping,
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

            foreach (var player in data.Players)
            {
                var firstName = GetCleanDbName(player.FirstNameId, data.FirstNames);
                var lastName = GetCleanDbName(player.LastNameId, data.LastNames);
                var commmonName = GetCleanDbName(player.CommonNameId, data.CommonNames);

                string[] keyParts =
                [
                    (commmonName == DBNull.Value ? $"{lastName}, {firstName}" : $"{commmonName}").Trim(),
                    player.WorldReputation.ToString(),
                    player.CurrentReputation.ToString(),
                    player.HomeReputation.ToString(),
                    player.CurrentAbility.ToString(),
                    player.PotentialAbility.ToString()
                ];

                command.Parameters["@id"].Value = player.Id;
                command.Parameters["@file_id"].Value = iFile;
                command.Parameters["@first_name"].Value = firstName;
                command.Parameters["@last_name"].Value = lastName;
                command.Parameters["@common_name"].Value = commmonName;
                command.Parameters["@date_of_birth"].Value = player.DateOfBirth;
                command.Parameters["@nation_id"].Value = GetMapDbIdObject(nationsMapping, iFile, player.NationId);
                command.Parameters["@secondary_nation_id"].Value = GetMapDbIdObject(nationsMapping, iFile, player.SecondaryNationId);
                command.Parameters["@caps"].Value = player.InternationalCaps;
                command.Parameters["@international_goals"].Value = player.InternationalGoals;
                command.Parameters["@right_foot"].Value = player.RightFoot;
                command.Parameters["@left_foot"].Value = player.LeftFoot;
                command.Parameters["@ability"].Value = player.CurrentAbility;
                command.Parameters["@potential_ability"].Value = player.PotentialAbility;
                command.Parameters["@home_reputation"].Value = player.HomeReputation;
                command.Parameters["@current_reputation"].Value = player.CurrentReputation;
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
                command.Parameters["@pos_goalkeeper"].Value = player.GoalKeeperPos;
                command.Parameters["@pos_sweeper"].Value = player.SweeperPos;
                command.Parameters["@pos_defender"].Value = player.DefenderPos;
                command.Parameters["@pos_defensive_midfielder"].Value = player.DefensiveMidfielderPos;
                command.Parameters["@pos_midfielder"].Value = player.MidfielderPos;
                command.Parameters["@pos_attacking_midfielder"].Value = player.AttackingMidfielderPos;
                command.Parameters["@pos_forward"].Value = player.StrikerPos;
                command.Parameters["@pos_wingback"].Value = player.WingBackPos;
                command.Parameters["@pos_free_role"].Value = player.FreeRolePos;
                command.Parameters["@side_left"].Value = player.LeftSide;
                command.Parameters["@side_right"].Value = player.RightSide;
                command.Parameters["@side_center"].Value = player.CentreSide;
                // TODO
                command.Parameters["@squad_status"].Value = DBNull.Value;
                command.Parameters["@transfer_status"].Value = DBNull.Value;

                command.Parameters["@anticipation"].Value = player.Anticipation;
                command.Parameters["@acceleration"].Value = player.Acceleration;
                command.Parameters["@adaptability"].Value = player.Adaptability;
                command.Parameters["@aggression"].Value = player.Aggression;
                command.Parameters["@agility"].Value = player.Agility;
                command.Parameters["@ambition"].Value = player.Ambition;
                command.Parameters["@balance"].Value = player.Balance;
                command.Parameters["@bravery"].Value = player.Bravery;
                command.Parameters["@consistency"].Value = player.Consistency;
                command.Parameters["@corners"].Value = player.Corners;
                command.Parameters["@creativity"].Value = player.Creativity;
                command.Parameters["@crossing"].Value = player.Crossing;
                command.Parameters["@decisions"].Value = player.Decisions;
                command.Parameters["@determination"].Value = player.Determination;
                command.Parameters["@dirtiness"].Value = player.Dirtiness;
                command.Parameters["@dribbling"].Value = player.Dribbling;
                command.Parameters["@finishing"].Value = player.Finishing;
                command.Parameters["@flair"].Value = player.Flair;
                command.Parameters["@handling"].Value = player.Handling;
                command.Parameters["@heading"].Value = player.Heading;
                command.Parameters["@important_matches"].Value = player.ImportantMatches;
                command.Parameters["@influence"].Value = player.Influence;
                command.Parameters["@injury_proneness"].Value = player.InjuryProneness;
                command.Parameters["@jumping"].Value = player.Jumping;
                command.Parameters["@long_shots"].Value = player.LongShots;
                command.Parameters["@loyalty"].Value = player.Loyalty;
                command.Parameters["@marking"].Value = player.Marking;
                command.Parameters["@natural_fitness"].Value = player.NaturalFitness;
                command.Parameters["@off_the_ball"].Value = player.OffTheBall;
                command.Parameters["@one_on_ones"].Value = player.OneOnOnes;
                command.Parameters["@pace"].Value = player.Pace;
                command.Parameters["@passing"].Value = player.Passing;
                command.Parameters["@penalties"].Value = player.Penalties;
                command.Parameters["@positioning"].Value = player.Positioning;
                command.Parameters["@pressure"].Value = player.Pressure;
                command.Parameters["@professionalism"].Value = player.Professionalism;
                command.Parameters["@reflexes"].Value = player.Reflexes;
                command.Parameters["@set_pieces"].Value = player.FreeKicks;
                command.Parameters["@sportsmanship"].Value = player.Sportsmanship;
                command.Parameters["@stamina"].Value = player.Stamina;
                command.Parameters["@strength"].Value = player.Strength;
                command.Parameters["@tackling"].Value = player.Tackling;
                command.Parameters["@teamwork"].Value = player.Teamwork;
                command.Parameters["@technique"].Value = player.Technique;
                command.Parameters["@temperament"].Value = player.Temperament;
                command.Parameters["@throw_ins"].Value = player.ThrowIns;
                command.Parameters["@versatility"].Value = player.Versatility;
                command.Parameters["@work_rate"].Value = player.WorkRate;

                command.ExecuteNonQuery();

                _reportProgress($"Player '{keyParts[0]}' has been created for file {fileName}.");
            }
            iFile++;
        }
    }

    private void SetClubsInformation(string[] saveFilePaths, List<SaveIdMapper> clubsMapping)
    {
        _reportProgress("Computes clubs information...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE clubs " +
            "SET rival_club_1 = @rival_club_1, rival_club_2 = @rival_club_2, rival_club_3 = @rival_club_3, " +
            "reputation = @reputation, bank = @bank, facilities = @facilities " +
            "WHERE id = @id";
        command.SetParameter("rival_club_1", DbType.Int32);
        command.SetParameter("rival_club_2", DbType.Int32);
        command.SetParameter("rival_club_3", DbType.Int32);
        command.SetParameter("reputation", DbType.Int32);
        command.SetParameter("bank", DbType.Int32);
        command.SetParameter("facilities", DbType.Int32);
        command.SetParameter("id", DbType.Int32);
        command.Prepare();

        void AddIfMatch(List<int> dbIdList, int saveId, int fileIndex)
        {
            if (saveId >= 0)
            {
                var match = clubsMapping.FirstOrDefault(x => x.SaveId.TryGetValue(fileIndex, out var currentId) && currentId == saveId);
                if (!match.Equals(default(SaveIdMapper)))
                {
                    dbIdList.Add(match.DbId);
                }
            }
        }

        object MacOrAvgOrNull(List<int> source, int count, bool isId)
        {
            var group = source.GetMaxOccurence(x => x);
            if (group is not null)
            {
                if (group.Count() >= Settings.MinValueOccurenceRate * count
                    || (isId && source.Count == count))
                {
                    return group.Key;
                }
                else if (!isId)
                {
                    return (int)Math.Round(source.Average());
                }
            }
            return DBNull.Value;
        }

        foreach (var clubIdMap in clubsMapping)
        {
            var keysCount = clubIdMap.SaveId.Keys.Count;

            var RivalClub1List = new List<int>(keysCount);
            var RivalClub2List = new List<int>(keysCount);
            var RivalClub3List = new List<int>(keysCount);
            var reputationList = new List<int>(keysCount);
            var facilitiesList = new List<int>(keysCount);
            var bankList = new List<int>(keysCount);

            foreach (var fileId in clubIdMap.SaveId.Keys)
            {
                var club = GetSaveGameDataFromCache(saveFilePaths[fileId])
                    .Clubs[clubIdMap.SaveId[fileId]];

                reputationList.Add(club.Reputation);
                facilitiesList.Add(club.Facilities);
                bankList.Add(club.Bank);

                AddIfMatch(RivalClub1List, club.RivalClub1, fileId);
                AddIfMatch(RivalClub2List, club.RivalClub2, fileId);
                AddIfMatch(RivalClub3List, club.RivalClub3, fileId);
            }

            command.Parameters["@id"].Value = clubIdMap.DbId;
            command.Parameters["@rival_club_1"].Value = MacOrAvgOrNull(RivalClub1List, keysCount, true);
            command.Parameters["@rival_club_2"].Value = MacOrAvgOrNull(RivalClub2List, keysCount, true);
            command.Parameters["@rival_club_3"].Value = MacOrAvgOrNull(RivalClub3List, keysCount, true);
            command.Parameters["@bank"].Value = MacOrAvgOrNull(bankList, keysCount, false);
            command.Parameters["@facilities"].Value = MacOrAvgOrNull(facilitiesList, keysCount, false);
            command.Parameters["@reputation"].Value = MacOrAvgOrNull(reputationList, keysCount, false);
            command.ExecuteNonQuery();

            _reportProgress($"Information updated for clubId: {clubIdMap.DbId}.");
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
                        SaveId = new Dictionary<int, int>
                        {
                            { iFile, sourceData[key].Id }
                        }
                    });

                    _reportProgress($"'{functionnalKey}' has been created in '{tableName}'.");
                }
                else
                {
                    match.SaveId.Add(iFile, sourceData[key].Id);
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
        => saveId < 0 ? -1 :mapping.First(x => x.SaveId.TryGetValue(fileIndex, out var currentSaveId) && currentSaveId == saveId).DbId;

    private static object GetCleanDbName(int nameId, Dictionary<int, string> names)
    {
        return names.TryGetValue(nameId, out var localName)
            && !string.IsNullOrWhiteSpace(localName)
            ? localName.Trim().Split(NameNewLineSeparators, StringSplitOptions.RemoveEmptyEntries).Last().Trim()
            : DBNull.Value;
    }

    private static string GetNameValue(int nameId, Dictionary<int, string> names)
    {
        return names.TryGetValue(nameId, out var localName)
            && !string.IsNullOrWhiteSpace(localName)
            ? localName.Trim().Split(NameNewLineSeparators, StringSplitOptions.RemoveEmptyEntries).Last().Trim()
            : string.Empty;
    }
}
