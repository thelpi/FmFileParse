﻿using System.Data;
using FmFileParse.Models;
using FmFileParse.Models.Internal;
using FmFileParse.SaveImport;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class DataImporter(Action<string> reportProgress)
{
    private static readonly string[] NameNewLineSeparators = ["\r\n", "\r", "\n"];

    private static readonly string[] PlayerTableStringColumns =
    [
        "first_name", "last_name", "common_name"
    ];

    private static readonly string[] PlayerTableDateColumns =
    [
        "date_of_birth", "contract_expiration"
    ];

    private static readonly string[] PlayerTableBoolColumns =
    [
        "leaving_on_bosman"
    ];

    private static readonly string[] PlayerTableNoAvgColumns =
    [
        "club_id", "nation_id", "secondary_nation_id", "future_club_id", "transfer_status", "squad_status"
    ];

    private static readonly string[] PlayerTableColumns =
    [
        // meta
        "occurences",
        // intrinsic
        "first_name", "last_name", "common_name", "date_of_birth", "right_foot", "left_foot",
        // nation related
        "nation_id", "secondary_nation_id", "caps", "international_goals",
        // potential & reputation
        "ability", "potential_ability", "home_reputation", "current_reputation", "world_reputation",
        // club related
        "club_id", "value", "contract_expiration", "wage",
        "manager_job_rel", "min_fee_rel", "non_play_rel", "non_promotion_rel", "relegation_rel",
        "leaving_on_bosman", "future_club_id", "transfer_status", "squad_status",
        // positions
        "pos_goalkeeper", "pos_sweeper", "pos_defender", "pos_defensive_midfielder", "pos_midfielder",
        "pos_attacking_midfielder", "pos_forward", "pos_wingback", "pos_free_role",
        // sides
        "side_left", "side_right", "side_center",
        // attributes
        "acceleration", "adaptability", "aggression", "agility", "ambition", "anticipation", "balance", "bravery",
        "consistency", "corners", "creativity", "crossing", "decisions", "determination", "dirtiness", "dribbling",
        "finishing", "flair", "handling", "heading", "important_matches", "influence", "injury_proneness", "jumping",
        "long_shots", "loyalty", "marking", "natural_fitness", "off_the_ball", "one_on_ones", "pace", "passing",
        "penalties", "positioning", "pressure", "professionalism", "reflexes", "set_pieces", "sportsmanship", "stamina",
        "strength", "tackling", "teamwork", "technique", "temperament", "throw_ins", "versatility", "work_rate",
        // potential attributes
        "anticipation_potential", "creativity_potential", "crossing_potential", "decisions_potential",
        "dribbling_potential", "finishing_potential", "handling_potential", "heading_potential",
        "long_shots_potential", "marking_potential", "off_the_ball_potential", "one_on_ones_potential",
        "passing_potential", "positioning_potential", "reflexes_potential", "tacking_potential",
        "penalties_potential", "throw_ins_potential"
    ];

    private static readonly (string name, bool hasAutoIncrement)[] Tables =
    [
        ("players", true),
        ("clubs", true),
        ("club_competitions", true),
        ("nations", true),
        ("confederations", true),
        ("players_merge_statistics", false),
        ("save_files_references", false)
    ];

    private static readonly List<(string tSource, string cSource, string tTarget, string cTarget)> SqlKeysInfo =
    [
        ("nations", "confederation_id", "confederations", "id"),
        ("club_competitions", "nation_id", "nations", "id"),
        ("clubs", "nation_id", "nations", "id"),
        ("clubs", "division_id", "club_competitions", "id"),
        ("clubs", "liked_staff_1", "players", "id"),
        ("clubs", "liked_staff_2", "players", "id"),
        ("clubs", "liked_staff_3", "players", "id"),
        ("clubs", "disliked_staff_1", "players", "id"),
        ("clubs", "disliked_staff_2", "players", "id"),
        ("clubs", "disliked_staff_3", "players", "id"),
        ("clubs", "rival_club_1", "clubs", "id"),
        ("clubs", "rival_club_2", "clubs", "id"),
        ("clubs", "rival_club_3", "clubs", "id"),
        ("players", "nation_id", "nations", "id"),
        ("players", "secondary_nation_id", "nations", "id"),
        ("players", "club_id", "clubs", "id"),
        ("players", "future_club_id", "clubs", "id"),
        ("players_merge_statistics", "player_id", "players", "id"),
    ];

    private readonly Func<MySqlConnection> _getConnection = () => new MySqlConnection(Settings.ConnString);
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
        SetClubsInformation(clubs, saveFilePaths);
        SetSaveFileReferences(clubs, nameof(Club));

        var players = ImportPlayers(saveFilePaths, nations, clubs);
        SetSaveFileReferences(players, nameof(Player));
        UpdateStaffOnClubs(players, saveFilePaths);

        CreateIndexesAndForeignKeys();
    }

    private void ClearAllData()
    {
        _reportProgress("Cleaning previous data starts...");

        DropIndexesAndForeignKeys();

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        foreach (var (name, oi) in Tables)
        {
            command.CommandText = $"TRUNCATE TABLE {name}";
            command.ExecuteNonQuery();

            if (oi)
            {
                command.CommandText = $"ALTER TABLE {name} AUTO_INCREMENT = 1";
                command.ExecuteNonQuery();
            }
        }
    }

    private void DropIndexesAndForeignKeys()
    {
        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        foreach (var (tSource, cSource, tTarget, cTarget) in SqlKeysInfo)
        {
            command.CommandText = $"ALTER TABLE {tSource} DROP FOREIGN KEY {string.Concat(tSource, "_", cSource, "_", tTarget)}";
            command.ExecuteNonQuerySecured();

            command.CommandText = $"ALTER TABLE {tSource} DROP INDEX {cSource}";
            command.ExecuteNonQuerySecured();
        }

        command.CommandText = "ALTER TABLE players_merge_statistics DROP PRIMARY KEY";
        command.ExecuteNonQuerySecured();

        command.CommandText = "ALTER TABLE save_files_references DROP PRIMARY KEY";
        command.ExecuteNonQuerySecured();
    }

    private void CreateIndexesAndForeignKeys()
    {
        _reportProgress("Creates indexes...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = "ALTER TABLE players_merge_statistics " +
            "ADD PRIMARY KEY(player_id, field)";
        command.ExecuteNonQuery();

        command.CommandText = "ALTER TABLE save_files_references " +
            "ADD PRIMARY KEY(data_type, data_id, file_id)";
        command.ExecuteNonQuery();

        foreach (var (tSource, cSource, tTarget, cTarget) in SqlKeysInfo)
        {
            command.CommandText = $"ALTER TABLE {tSource} " +
                $"ADD INDEX ({cSource})";
            command.ExecuteNonQuery();

            command.CommandText = $"ALTER TABLE {tSource} " +
                $"ADD CONSTRAINT {string.Concat(tSource, "_", cSource, "_", tTarget)} FOREIGN KEY ({cSource}) " +
                $"REFERENCES {tTarget}({cTarget}) ON DELETE RESTRICT ON UPDATE RESTRICT";
            command.ExecuteNonQuery();
        }
    }

    private void SetSaveFileReferences(List<SaveIdMapper> data, string dataTypeName)
    {
        _reportProgress("Saves savefiles references map...");

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
            command.CommandText = $"INSERT INTO save_files_references (data_type, data_id, file_id, save_id) " +
                $"VALUES {string.Join(", ", sqlRowValues)}";
            command.ExecuteNonQuery();
        }
    }

    private List<SaveIdMapper> ImportConfederations(string[] saveFilePaths)
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

    private List<SaveIdMapper> ImportNations(string[] saveFilePaths,
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
                ("confederation_id", DbType.Int32, (d, iFile) => GetMapDbId(confederationsMapping, iFile, d.ConfederationId).DbNullIf(-1)),
            },
            (d, iFile) => string.Concat(d.Name, ";", GetMapDbId(confederationsMapping, iFile, d.ConfederationId)));

        return nations;
    }

    private List<SaveIdMapper> ImportClubCompetitions(string[] saveFilePaths,
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
                ("nation_id", DbType.Int32, (d, iFile) => GetMapDbId(nationsMapping, iFile, d.NationId).DbNullIf(-1)),
                ("reputation", DbType.Int32, (d, iFile) => d.Reputation)
            },
            (d, iFile) => string.Concat(d.LongName, ";", GetMapDbId(nationsMapping, iFile, d.NationId)));
    }

    private List<SaveIdMapper> ImportClubs(string[] saveFilePaths,
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
                ("nation_id", DbType.Int32, (d, iFile) => GetMapDbId(nationsMapping, iFile, d.NationId).DbNullIf(-1)),
                ("division_id", DbType.Int32, (d, iFile) => GetMapDbId(clubCompetitionsMapping, iFile, d.DivisionId).DbNullIf(-1))
            },
            // note: the key here should be the same as the one used in 'DataFileLoaders.ManageDuplicateClubs'
            (d, iFile) => string.Concat(d.LongName, ";", GetMapDbId(nationsMapping, iFile, d.NationId), ";", GetMapDbId(clubCompetitionsMapping, iFile, d.DivisionId)));
    }

    private void UpdateStaffOnClubs(List<SaveIdMapper> playersMapping, string[] saveFilePaths)
    {
        _reportProgress("Updates club's staff information...");

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

        var keysByPlayer = new Dictionary<int, List<int[]>>(playersMapping.Count);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM save_files_references WHERE data_type = @data_type ORDER BY data_id, file_id";
        command.SetParameter("@data_type", DbType.String, nameof(Club));
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var fileId = reader.GetInt32("file_id");
            var playerDbId = reader.GetInt32("data_id");
            var playerSaveId = reader.GetInt32("save_id");

            if (!keysByPlayer.TryGetValue(playerDbId, out var keys))
            {
                keys = new List<int[]>(saveFilePaths.Length);
                keysByPlayer.Add(playerDbId, keys);
            }

            var club = GetSaveGameDataFromCache(saveFilePaths[fileId]).Clubs[playerSaveId];

            keys.Add(
            [
                GetMapDbIdIfAny(playersMapping, fileId, club.LikedStaff1),
                GetMapDbIdIfAny(playersMapping, fileId, club.LikedStaff2),
                GetMapDbIdIfAny(playersMapping, fileId, club.LikedStaff3),
                GetMapDbIdIfAny(playersMapping, fileId, club.DislikedStaff1),
                GetMapDbIdIfAny(playersMapping, fileId, club.DislikedStaff2),
                GetMapDbIdIfAny(playersMapping, fileId, club.DislikedStaff3),
            ]);
        }

        foreach (var pid in keysByPlayer.Keys)
        {
            var maxxedOccurences = new int[6];
            for (var i = 0; i < 6; i++)
                maxxedOccurences[i] = keysByPlayer[pid].GetMaxOccurence(x => x[i]).Key;

            if (maxxedOccurences.Any(v => v != -1))
            {
                wCommand.Parameters["@liked_staff_1"].Value = maxxedOccurences[0].DbNullIf(-1);
                wCommand.Parameters["@liked_staff_2"].Value = maxxedOccurences[1].DbNullIf(-1);
                wCommand.Parameters["@liked_staff_3"].Value = maxxedOccurences[2].DbNullIf(-1);
                wCommand.Parameters["@disliked_staff_1"].Value = maxxedOccurences[3].DbNullIf(-1);
                wCommand.Parameters["@disliked_staff_2"].Value = maxxedOccurences[4].DbNullIf(-1);
                wCommand.Parameters["@disliked_staff_3"].Value = maxxedOccurences[5].DbNullIf(-1);
                wCommand.Parameters["@id"].Value = pid;
                wCommand.ExecuteNonQuery();
            }
        }
    }

    private void SetClubsInformation(List<SaveIdMapper> clubsMapping, string[] saveFilePaths)
    {
        _reportProgress("Computes clubs aggregated information...");

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
            var dbId = GetMapDbIdIfAny(clubsMapping, fileIndex, saveId);
            if (dbId >= 0)
            {
                dbIdList.Add(dbId);
            }
        }

        object MaxOrAvgOrNull(List<int> source, int count, bool isId)
        {
            if (source.Count == 0)
            {
                return DBNull.Value;
            }

            var group = source.GetMaxOccurence(x => x);
            return group.Count() >= Settings.MinValueOccurenceRate * count
                || (isId && source.Count == count)
                ? group.Key
                : !isId
                    ? (int)Math.Round(source.Average())
                    : DBNull.Value;
        }

        var clubCount = 0;
        foreach (var clubIdMap in clubsMapping)
        {
            var keysCount = clubIdMap.SaveId.Keys.Count;

            var rivalClub1List = new List<int>(keysCount);
            var rivalClub2List = new List<int>(keysCount);
            var rivalClub3List = new List<int>(keysCount);
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

                AddIfMatch(rivalClub1List, club.RivalClub1, fileId);
                AddIfMatch(rivalClub2List, club.RivalClub2, fileId);
                AddIfMatch(rivalClub3List, club.RivalClub3, fileId);
            }

            command.Parameters["@id"].Value = clubIdMap.DbId;
            command.Parameters["@rival_club_1"].Value = MaxOrAvgOrNull(rivalClub1List, keysCount, true);
            command.Parameters["@rival_club_2"].Value = MaxOrAvgOrNull(rivalClub2List, keysCount, true);
            command.Parameters["@rival_club_3"].Value = MaxOrAvgOrNull(rivalClub3List, keysCount, true);
            command.Parameters["@bank"].Value = MaxOrAvgOrNull(bankList, keysCount, false);
            command.Parameters["@facilities"].Value = MaxOrAvgOrNull(facilitiesList, keysCount, false);
            command.Parameters["@reputation"].Value = MaxOrAvgOrNull(reputationList, keysCount, false);
            command.ExecuteNonQuery();

            clubCount++;
            _reportProgress($"Information updated for club {clubCount} of {clubsMapping.Count}.");
        }
    }

    private List<SaveIdMapper> ImportPlayers(
        string[] saveFilePaths,
        List<SaveIdMapper> nationsMapping,
        List<SaveIdMapper> clubsMapping)
    {
        _reportProgress("Players importation starts...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = PlayerTableColumns.GetInsertQuery("players");
        foreach (var c in PlayerTableColumns)
        {
            command.SetParameter(c, GetDbType(c));
        }
        command.Prepare();

        var collectedMergeStats = new Dictionary<int, List<(string field, int occurences, MergeType mergeType)>>(520);
        var collectedDbIdMap = new List<SaveIdMapper>(12000);

        var names = new HashSet<string>(12000);
        var allPlayers = new Dictionary<string, Dictionary<int, List<(Player player, int fileId)>>>(12000 * saveFilePaths.Length);
        for (var fileId = 0; fileId < saveFilePaths.Length; fileId++)
        {
            var data = GetSaveGameDataFromCache(saveFilePaths[fileId]);

            foreach (var p in data.Players)
            {
                var firstName = GetNameValue(p.FirstNameId, data.FirstNames);
                var lastName = GetNameValue(p.LastNameId, data.LastNames);
                var commmonName = GetNameValue(p.CommonNameId, data.CommonNames);

                var playerKey = $"{firstName};{lastName};{commmonName}";
                names.Add(playerKey);

                if (!allPlayers.TryGetValue(playerKey, out var filesPlayers))
                {
                    filesPlayers = [];
                    allPlayers.Add(playerKey, filesPlayers);
                }

                if (!filesPlayers.TryGetValue(fileId, out var filePlayers))
                {
                    filePlayers = [];
                    filesPlayers.Add(fileId, filePlayers);
                }

                filePlayers.Add((p, fileId));
            }
        }

        foreach (var playerKey in names)
        {
            var playersByName = allPlayers[playerKey].SelectMany(x => x.Value).ToList();
            if (allPlayers[playerKey].Any(x => x.Value.Count > 1))
            {
                foreach (var dob in playersByName.Select(x => x.player.DateOfBirth).Distinct())
                {
                    var playersByDob = playersByName.Where(x => x.player.DateOfBirth == dob).ToList();
                    if (playersByDob.GroupBy(x => x.fileId).Any(x => x.Count() > 1))
                    {
                        foreach (var club in playersByDob.Select(x => GetMapDbId(clubsMapping, x.fileId, x.player.ClubId)).Distinct())
                        {
                            var playersByClub = playersByDob.Where(x => GetMapDbId(clubsMapping, x.fileId, x.player.ClubId) == club).ToList();
                            if (playersByClub.GroupBy(x => x.fileId).Any(x => x.Count() > 1))
                            {
                                throw new NotSupportedException("Criteria on name, date of birth and club are not enough to distinguish players.");
                            }
                            else
                            {
                                var pMap = InsertMergedPlayer(playersByClub, command, saveFilePaths, nationsMapping, clubsMapping, collectedMergeStats);
                                if (pMap.HasValue)
                                {
                                    collectedDbIdMap.Add(pMap.Value);
                                }
                            }
                        }
                    }
                    else
                    {
                        var pMap = InsertMergedPlayer(playersByDob, command, saveFilePaths, nationsMapping, clubsMapping, collectedMergeStats);
                        if (pMap.HasValue)
                        {
                            collectedDbIdMap.Add(pMap.Value);
                        }
                    }
                }
            }
            else
            {
                var pMap = InsertMergedPlayer(playersByName, command, saveFilePaths, nationsMapping, clubsMapping, collectedMergeStats);
                if (pMap.HasValue)
                {
                    collectedDbIdMap.Add(pMap.Value);
                }
            }

            if (Settings.InsertStatistics && collectedMergeStats.Count >= 500)
            {
                BulkInsertPlayerMergeStatistics(collectedMergeStats);
            }
        }

        if (Settings.InsertStatistics && collectedMergeStats.Count > 0)
        {
            BulkInsertPlayerMergeStatistics(collectedMergeStats);
        }

        return collectedDbIdMap;
    }

    private SaveIdMapper? InsertMergedPlayer(
        List<(Player player, int fileId)> players,
        MySqlCommand insertPlayerCommand,
        string[] saveFilePaths,
        List<SaveIdMapper> nationsMapping,
        List<SaveIdMapper> clubsMapping,
        Dictionary<int, List<(string field, int occurences, MergeType mergeType)>> collectedMergeStats)
    {
        // there's not enough data across all files for the player
        if (players.Count / (decimal)saveFilePaths.Length < Settings.MinPlayerOccurencesRate)
        {
            _reportProgress($"The player has not enough data to be merged.");
            return null;
        }

        var columnsAndValues = new List<Dictionary<string, object>>(players.Count);
        var collectedSaveIds = new Dictionary<int, int>(players.Count);
        foreach (var (player, fileId) in players)
        {
            var data = GetSaveGameDataFromCache(saveFilePaths[fileId]);

            var singleFilePlayerData = new Dictionary<string, object>
            {
                { "first_name", GetNameValue(player.FirstNameId, data.FirstNames).DbNullIf(string.Empty) },
                { "last_name", GetNameValue(player.LastNameId, data.LastNames).DbNullIf(string.Empty) },
                { "common_name", GetNameValue(player.CommonNameId, data.CommonNames).DbNullIf(string.Empty) },
                { "date_of_birth", player.DateOfBirth },
                { "nation_id", GetMapDbId(nationsMapping, fileId, player.NationId).DbNullIf(-1) },
                { "secondary_nation_id", GetMapDbId(nationsMapping, fileId, player.SecondaryNationId).DbNullIf(-1) },
                { "caps", player.InternationalCaps },
                { "international_goals", player.InternationalGoals },
                { "right_foot", player.RightFoot },
                { "left_foot", player.LeftFoot },
                { "ability", player.CurrentAbility },
                { "potential_ability", player.PotentialAbility },
                { "home_reputation", player.HomeReputation },
                { "current_reputation", player.CurrentReputation },
                { "world_reputation", player.WorldReputation },
                { "club_id", GetMapDbId(clubsMapping, fileId, player.ClubId).DbNullIf(-1) },
                { "value", player.Value },
                { "contract_expiration", (player.Contract?.ContractEndDate).DbNullIf() },
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
                { "transfer_status", player.Contract?.TransferStatus is not null ? (int)player.Contract.TransferStatus.Value : DBNull.Value },
                { "squad_status", player.Contract?.SquadStatus  is not null ? (int)player.Contract.SquadStatus.Value : DBNull.Value },
                { "leaving_on_bosman", player.Contract?.LeavingOnBosman ?? false },
                { "future_club_id", GetMapDbIdIfAny(clubsMapping, fileId, player.Contract?.FutureClubId ?? -1).DbNullIf(-1) },
                { "side_left", player.LeftSide },
                { "side_right", player.RightSide },
                { "side_center", player.CentreSide },
                { "anticipation", player.Anticipation.current },
                { "acceleration", player.Acceleration },
                { "adaptability", player.Adaptability },
                { "aggression", player.Aggression },
                { "agility", player.Agility },
                { "ambition", player.Ambition },
                { "balance", player.Balance },
                { "bravery", player.Bravery },
                { "consistency", player.Consistency },
                { "corners", player.Corners },
                { "creativity", player.Creativity.current },
                { "crossing", player.Crossing.current },
                { "decisions", player.Decisions.current },
                { "determination", player.Determination },
                { "dirtiness", player.Dirtiness },
                { "dribbling", player.Dribbling.current },
                { "finishing", player.Finishing.current },
                { "flair", player.Flair },
                { "handling", player.Handling.current },
                { "heading", player.Heading.current },
                { "important_matches", player.ImportantMatches },
                { "influence", player.Influence },
                { "injury_proneness", player.InjuryProneness },
                { "jumping", player.Jumping },
                { "long_shots", player.LongShots.current },
                { "loyalty", player.Loyalty },
                { "marking", player.Marking.current },
                { "natural_fitness", player.NaturalFitness },
                { "off_the_ball", player.OffTheBall.current },
                { "one_on_ones", player.OneOnOnes.current },
                { "pace", player.Pace },
                { "passing", player.Passing.current },
                { "penalties", player.Penalties.current },
                { "positioning", player.Positioning.current },
                { "pressure", player.Pressure },
                { "professionalism", player.Professionalism },
                { "reflexes", player.Reflexes.current },
                { "set_pieces", player.FreeKicks },
                { "sportsmanship", player.Sportsmanship },
                { "stamina", player.Stamina },
                { "strength", player.Strength },
                { "tackling", player.Tackling.current },
                { "teamwork", player.Teamwork },
                { "technique", player.Technique },
                { "temperament", player.Temperament },
                { "throw_ins", player.ThrowIns.current },
                { "versatility", player.Versatility },
                { "work_rate", player.WorkRate },
                { "anticipation_potential", player.Anticipation.potential },
                { "creativity_potential", player.Creativity.potential },
                { "crossing_potential", player.Crossing.potential },
                { "decisions_potential", player.Decisions.potential },
                { "dribbling_potential", player.Dribbling.potential },
                { "finishing_potential", player.Finishing.potential },
                { "handling_potential", player.Handling.potential },
                { "heading_potential", player.Heading.potential },
                { "long_shots_potential", player.LongShots.potential },
                { "marking_potential", player.Marking.potential },
                { "off_the_ball_potential", player.OffTheBall.potential },
                { "one_on_ones_potential", player.OneOnOnes.potential },
                { "passing_potential", player.Passing.potential },
                { "positioning_potential", player.Positioning.potential },
                { "reflexes_potential", player.Reflexes.potential },
                { "tacking_potential", player.Tackling.potential },
                { "penalties_potential", player.Penalties.potential },
                { "throw_ins_potential", player.ThrowIns.potential }
            };

            columnsAndValues.Add(singleFilePlayerData);
            collectedSaveIds.Add(fileId, player.Id);
        }

        // we can extract the list of columns from the first item; other items will have the same
        var columns = columnsAndValues[0].Keys;
        
        var colsStats = new List<(string field, int distinctOccurences, MergeType mergeType)>(columns.Count);
        var colsAndVals = new Dictionary<string, object>(columns.Count);
        foreach (var col in columns)
        {
            Func<IEnumerable<object>, object>? averageFunc = null;
            if (PlayerTableDateColumns.Contains(col))
            {
                averageFunc = values => values.Select(Convert.ToDateTime).Average();
            }
            else if (!PlayerTableStringColumns.Contains(col) && !PlayerTableBoolColumns.Contains(col) && !PlayerTableNoAvgColumns.Contains(col))
            {
                averageFunc = values => (int)Math.Round(values.Select(Convert.ToInt32).Average());
            }

            var (computedValue, mergeType, countValues) = CrawlValuesForMerge(
                columnsAndValues,
                columnsAndValues.Select(_ => _[col]),
                averageFunc);

            colsAndVals.Add(col, computedValue);
            colsStats.Add((col, countValues, mergeType));
        }

        colsAndVals.Add("occurences", columnsAndValues.Count);

        foreach (var c in colsAndVals.Keys)
        {
            insertPlayerCommand.Parameters[$"@{c}"].Value = colsAndVals[c];
        }
        insertPlayerCommand.ExecuteNonQuery();

        var dbPlayerId = (int)insertPlayerCommand.LastInsertedId;

        collectedMergeStats.Add(dbPlayerId, colsStats);

        _reportProgress($"The player has been merged.");

        return new SaveIdMapper
        {
            DbId = dbPlayerId,
            SaveId = collectedSaveIds
        };
    }

    private static (object computedValue, MergeType mergeType, int countValues) CrawlValuesForMerge(
        List<Dictionary<string, object>> allFilePlayerData,
        IEnumerable<object> allValues,
        Func<IEnumerable<object>, object>? averageFunc)
    {
        // note: there's an arbitrary choice when the two first occurences have the same count
        var groups = allValues
            .GroupBy(x => x)
            .Select(x => (Value: x.Key, Count: x.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        return groups[0].Count >= Settings.MinValueOccurenceRate * allFilePlayerData.Count
            ? (groups[0].Value, MergeType.ModeAboveThreshold, groups.Count)
                : (averageFunc is not null && !groups.Any(x => x.Value == DBNull.Value)
                    ? (averageFunc(allValues), MergeType.Average, groups.Count)
                    : (groups[0].Value, MergeType.ModeBelowThreshold, groups.Count));
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

    #region reusable

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

    private SaveGameData GetSaveGameDataFromCache(string saveFilePath)
    {
        if (!_loadedSaveData.TryGetValue(saveFilePath, out var data))
        {
            data = SaveGameHandler.OpenSaveGameIntoMemory(saveFilePath);
            _loadedSaveData.Add(saveFilePath, data);
        }

        return data;
    }

    private static int GetMapDbId(List<SaveIdMapper> mapping, int fileIndex, int saveId)
        => saveId < 0 ? -1 : mapping.First(x => x.SaveId.TryGetValue(fileIndex, out var currentSaveId) && currentSaveId == saveId).DbId;

    private static int GetMapDbIdIfAny(List<SaveIdMapper> mapping, int fileIndex, int saveId)
    {
        if (saveId < 0)
        {
            return -1;
        }

        var match = mapping.FirstOrDefault(x => x.SaveId.TryGetValue(fileIndex, out var currentSaveId) && currentSaveId == saveId);
        return match.Equals(default(SaveIdMapper)) ? -1 : match.DbId;
    }

    private static string GetNameValue(int nameId, Dictionary<int, string> names)
    {
        return names.TryGetValue(nameId, out var localName)
            && !string.IsNullOrWhiteSpace(localName)
            ? localName.Trim().Split(NameNewLineSeparators, StringSplitOptions.RemoveEmptyEntries).Last().Trim()
            : string.Empty;
    }

    private static DbType GetDbType(string column)
    {
        return PlayerTableStringColumns.Contains(column)
            ? DbType.String
            : (PlayerTableDateColumns.Contains(column)
                ? DbType.Date
                : (PlayerTableBoolColumns.Contains(column)
                    ? DbType.Boolean
                    : DbType.Int32));
    }

    #endregion
}
