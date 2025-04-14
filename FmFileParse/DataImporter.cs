﻿using System.Data;
using FmFileParse.Models;
using FmFileParse.Models.Internal;
using FmFileParse.SaveImport;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class DataImporter(Action<string> reportProgress)
{
    private static readonly string[] PlayerTableStringColumns =
    [
        "first_name", "last_name", "common_name"
    ];

    private static readonly string[] PlayerTableDateColumns =
    [
        "date_of_birth", "contract_expiration"
    ];

    private static readonly string[] PlayerTableColumns =
    [
        // intrinsic
        "first_name", "last_name", "common_name", "date_of_birth", "right_foot", "left_foot",
        // nation related
        "nation_id", "secondary_nation_id", "caps", "international_goals",
        // potential & reputation
        "ability", "potential_ability", "home_reputation", "current_reputation", "world_reputation",
        // club related
        "club_id", "value", "contract_expiration", "wage",
        "manager_job_rel", "min_fee_rel", "non_play_rel", "non_promotion_rel", "relegation_rel",
        "transfer_status", "squad_status",
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

    private static readonly string[] Tables =
    [
        "players",
        "clubs",
        "club_competitions",
        "nations",
        "confederations"
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
        ("players", "club_id", "clubs", "id")
    ];

    private readonly Func<MySqlConnection> _getConnection = () => new MySqlConnection(Settings.ConnString);
    private readonly Dictionary<string, BaseFileData> _loadedSaveData = [];
    private readonly Action<string> _reportProgress = reportProgress;

    public void ProceedToImport(string[] saveFilePaths, bool playerOnly)
    {
        ClearAllData(playerOnly);

        List<SaveIdMapper> nations;
        List<SaveIdMapper> clubs;

        if (playerOnly)
        {
            nations = LoadMapping<Nation>();
            clubs = LoadMapping<Club>();
        }
        else
        {
            var confederations = ImportConfederations(saveFilePaths);
            SetSaveFileReferences(confederations, nameof(Confederation));

            nations = ImportNations(saveFilePaths, confederations);
            SetSaveFileReferences(nations, nameof(Nation));

            var clubCompetitions = ImportClubCompetitions(saveFilePaths, nations);
            SetSaveFileReferences(clubCompetitions, nameof(ClubCompetition));

            clubs = ImportClubs(saveFilePaths, nations, clubCompetitions);
            UpdatesRivalClubsOnClubs(clubs);
            SetSaveFileReferences(clubs, nameof(Club));
        }

        var players = ImportPlayers(saveFilePaths, nations, clubs);
        UpdateStaffPreferencesOnClubs(clubs, players);

        CreateIndexesAndForeignKeys(playerOnly);
    }

    private void ClearAllData(bool playerOnly)
    {
        _reportProgress("Cleaning previous data starts...");

        DropIndexesAndForeignKeys(playerOnly);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        foreach (var name in Tables)
        {
            if (!playerOnly || name == "players")
            {
                command.CommandText = $"TRUNCATE TABLE {name}";
                command.ExecuteNonQuery();

                command.CommandText = $"ALTER TABLE {name} AUTO_INCREMENT = 1";
                command.ExecuteNonQuery();
            }
        }

        if (playerOnly)
        {
            command.CommandText = "UPDATE clubs " +
                "SET liked_staff_1 = NULL, liked_staff_2 = NULL, liked_staff_3 = NULL, " +
                "disliked_staff_1 = NULL, disliked_staff_2 = NULL, disliked_staff_3 = NULL";
            command.ExecuteNonQuery();

            command.CommandText = "DELETE FROM save_files_references WHERE data_type = @data_type";
            command.SetParameter("@data_type", DbType.String, nameof(Player));
            command.ExecuteNonQuery();
        }
        else
        {
            command.CommandText = "TRUNCATE TABLE save_files_references";
            command.ExecuteNonQuery();
        }
    }

    private void DropIndexesAndForeignKeys(bool playerOnly)
    {
        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        foreach (var (tSource, cSource, tTarget, _) in SqlKeysInfo)
        {
            if (!playerOnly || tSource == "players" || tTarget == "players")
            {
                command.CommandText = $"ALTER TABLE {tSource} DROP FOREIGN KEY {string.Concat(tSource, "_", cSource, "_", tTarget)}";
                command.ExecuteNonQuerySecured();
            }

            if (!playerOnly || tSource == "players")
            {
                command.CommandText = $"ALTER TABLE {tSource} DROP INDEX {cSource}";
                command.ExecuteNonQuerySecured();
            }
        }
    }

    private void CreateIndexesAndForeignKeys(bool playerOnly)
    {
        _reportProgress("Creates indexes...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        foreach (var (tSource, cSource, tTarget, cTarget) in SqlKeysInfo)
        {
            if (!playerOnly || tSource == "players")
            {
                command.CommandText = $"ALTER TABLE {tSource} " +
                    $"ADD INDEX ({cSource})";
                command.ExecuteNonQuery();
            }
            
            if (!playerOnly || tSource == "players" || tTarget == "players")
            {
                command.CommandText = $"ALTER TABLE {tSource} " +
                    $"ADD CONSTRAINT {string.Concat(tSource, "_", cSource, "_", tTarget)} FOREIGN KEY ({cSource}) " +
                    $"REFERENCES {tTarget}({cTarget}) ON DELETE RESTRICT ON UPDATE RESTRICT";
                command.ExecuteNonQuery();
            }
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

    private List<SaveIdMapper> LoadMapping<T>()
        where T : BaseData
    {
        var mapping = new List<SaveIdMapper>(1000);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM save_files_references WHERE data_type = @data_type ORDER BY data_id";
        command.SetParameter("@data_type", DbType.String, typeof(T).Name);

        var currentMap = new SaveIdMapper { DbId = -1 };
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetInt32("data_id") != currentMap.DbId)
            {
                currentMap = new SaveIdMapper
                {
                    DbId = reader.GetInt32("data_id"),
                    SaveId = []
                };
                mapping.Add(currentMap);
            }

            currentMap.SaveId.Add(reader.GetInt32("file_id"), reader.GetInt32("save_id"));
        }

        return mapping;
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
            (d, _) => d.Acronym);
    }

    private List<SaveIdMapper> ImportNations(string[] saveFilePaths,
        List<SaveIdMapper> confederationsMapping)
    {
        return ImportData(x => x.Nations,
            saveFilePaths,
            "nations",
            new (string, DbType, Func<Nation, int, object>)[]
            {
                ("name", DbType.String, (d, _) => d.Name),
                ("is_eu", DbType.Boolean, (d, _) => d.IsEu),
                ("reputation", DbType.Int32, (d, _) => d.Reputation),
                ("league_standard", DbType.Int32, (d, _) => d.LeagueStandard),
                ("acronym", DbType.String, (d, _) => d.Acronym),
                ("confederation_id", DbType.Int32, (d, iFile) => GetMapDbId(confederationsMapping, iFile, d.ConfederationId).DbNullIf(-1)),
            },
            (d, iFile) => string.Concat(d.Name, ";", GetMapDbId(confederationsMapping, iFile, d.ConfederationId)));
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
                ("name", DbType.String, (c, iFile) => c.Name),
                ("long_name", DbType.String, (c, iFile) => c.LongName),
                ("nation_id", DbType.Int32, (c, iFile) => GetMapDbId(nationsMapping, iFile, c.NationId).DbNullIf(-1)),
                ("division_id", DbType.Int32, (c, iFile) => GetMapDbId(clubCompetitionsMapping, iFile, c.DivisionId).DbNullIf(-1)),
                ("reputation", DbType.Int32, (c, iFile) => c.Reputation),
                ("bank", DbType.Int32, (c, iFile) => c.Bank),
                ("facilities", DbType.Int32, (c, iFile) => c.Facilities)
            },
            // note: the key here should be the same as the one used in 'DataFileLoaders.ManageDuplicateClubs'
            (d, iFile) => string.Concat(d.LongName, ";", GetMapDbId(nationsMapping, iFile, d.NationId)));
    }

    private void UpdateStaffPreferencesOnClubs(List<SaveIdMapper> clubsMapping, List<SaveIdMapper> playersMapping)
    {
        _reportProgress("Updates club's staff information...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE clubs " +
            "SET liked_staff_1 = @liked_staff_1, liked_staff_2 = @liked_staff_2, liked_staff_3 = @liked_staff_3, " +
            "disliked_staff_1 = @disliked_staff_1, disliked_staff_2 = @disliked_staff_2, disliked_staff_3 = @disliked_staff_3 " +
            "WHERE id = @id";
        command.SetParameter("liked_staff_1", DbType.Int32);
        command.SetParameter("liked_staff_2", DbType.Int32);
        command.SetParameter("liked_staff_3", DbType.Int32);
        command.SetParameter("disliked_staff_1", DbType.Int32);
        command.SetParameter("disliked_staff_2", DbType.Int32);
        command.SetParameter("disliked_staff_3", DbType.Int32);
        command.SetParameter("id", DbType.Int32);
        command.Prepare();

        var dbMapPlayers = playersMapping
            .Where(x => x.SaveId.ContainsKey(0))
            .ToDictionary(x => x.SaveId[0], x => x.DbId);

        foreach (var clubMap in clubsMapping)
        {
            var dbClub = GetDbFileDataFromCache().Clubs[clubMap.SaveId[0]];

            command.Parameters["@liked_staff_1"].Value = (dbMapPlayers.TryGetValue(dbClub.LikedStaff1, out var dbId1) ? dbId1 : -1).DbNullIf(-1);
            command.Parameters["@liked_staff_2"].Value = (dbMapPlayers.TryGetValue(dbClub.LikedStaff2, out var dbId2) ? dbId2 : -1).DbNullIf(-1);
            command.Parameters["@liked_staff_3"].Value = (dbMapPlayers.TryGetValue(dbClub.LikedStaff3, out var dbId3) ? dbId3 : -1).DbNullIf(-1);
            command.Parameters["@disliked_staff_1"].Value = (dbMapPlayers.TryGetValue(dbClub.DislikedStaff1, out var dbId4) ? dbId4 : -1).DbNullIf(-1);
            command.Parameters["@disliked_staff_2"].Value = (dbMapPlayers.TryGetValue(dbClub.DislikedStaff2, out var dbId5) ? dbId5 : -1).DbNullIf(-1);
            command.Parameters["@disliked_staff_3"].Value = (dbMapPlayers.TryGetValue(dbClub.DislikedStaff3, out var dbId6) ? dbId6 : -1).DbNullIf(-1);
            command.Parameters["@id"].Value = clubMap.DbId;
            command.ExecuteNonQuery();
        }
    }

    private void UpdatesRivalClubsOnClubs(List<SaveIdMapper> clubsMapping)
    {
        _reportProgress("Updates rival clubs on clubs...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE clubs " +
            "SET rival_club_1 = @rival_club_1, rival_club_2 = @rival_club_2, rival_club_3 = @rival_club_3 " +
            "WHERE id = @id";
        command.SetParameter("rival_club_1", DbType.Int32);
        command.SetParameter("rival_club_2", DbType.Int32);
        command.SetParameter("rival_club_3", DbType.Int32);
        command.SetParameter("id", DbType.Int32);
        command.Prepare();

        var clubCount = 0;
        foreach (var clubIdMap in clubsMapping)
        {
            var dbClubs = GetDbFileDataFromCache().Clubs;
            var dbClub = dbClubs[clubIdMap.SaveId[0]];

            command.Parameters["@id"].Value = clubIdMap.DbId;
            command.Parameters["@rival_club_1"].Value = dbClubs.TryGetValue(dbClub.RivalClub1, out var value1)
                ? value1.Id
                : DBNull.Value;
            command.Parameters["@rival_club_2"].Value = dbClubs.TryGetValue(dbClub.RivalClub2, out var value2)
                ? value2.Id
                : DBNull.Value;
            command.Parameters["@rival_club_3"].Value = dbClubs.TryGetValue(dbClub.RivalClub3, out var value3)
                ? value3.Id
                : DBNull.Value;
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

        var collectedDbIdMap = new List<SaveIdMapper>(20000);

        var nationsMapRebind = nationsMapping
            .SelectMany(x => x.SaveId.Select(kvp => (kvp.Key, kvp.Value, x.DbId)))
            .ToDictionary(x => (x.Key, x.Value), x => x.DbId);

        var clubsMapRebind = clubsMapping
            .SelectMany(x => x.SaveId.Select(kvp => (kvp.Key, kvp.Value, x.DbId)))
            .ToDictionary(x => (x.Key, x.Value), x => x.DbId);

        // TODO: ugly count
        var namesFromSaves = new HashSet<(string, string, string)>(20000);
        var saveFilesPlayers = new Dictionary<int, List<Player>>(saveFilePaths.Length);
        for (var i = 1; i <= saveFilePaths.Length; i++)
        {
            var savePlayers = GetSaveGameDataFromCache(saveFilePaths[i - 1]).Players;

            foreach (var p in savePlayers)
            {
                RebindPlayerIds(p, i, nationsMapRebind, clubsMapRebind);
                namesFromSaves.Add((p.FirstName, p.LastName, p.CommonName));
            }

            saveFilesPlayers.Add(i, savePlayers);
        }

        var dbPlayers = new List<Player>(namesFromSaves.Count);
        foreach (var p in GetDbFileDataFromCache().Players)
        {
            if (namesFromSaves.Contains((p.FirstName, p.LastName, p.CommonName)))
            {
                RebindPlayerIds(p, 0, nationsMapRebind, clubsMapRebind);
                dbPlayers.Add(p);
            }
        }

        var keyFuncs = new List<Func<Player, object>>
        {
            x => (x.CommonName, x.LastName, x.FirstName),
            x => (x.CommonName, x.LastName, x.FirstName, x.ActualYearOfBirth),
            x => (x.CommonName, x.LastName, x.FirstName, x.ActualYearOfBirth, x.DateOfBirth),
            x => (x.CommonName, x.LastName, x.FirstName, x.ActualYearOfBirth, x.DateOfBirth, x.ClubId)
        };

        CrawlPlayers(dbPlayers, 0, keyFuncs, saveFilesPlayers, collectedDbIdMap, command);

        return collectedDbIdMap;
    }

    private static void CrawlPlayers(List<Player> sourceDbPlayers,
        int depth,
        List<Func<Player, object>> keyFuncs,
        Dictionary<int, List<Player>> saveFilesPlayers,
        List<SaveIdMapper> collectedDbIdMap,
        MySqlCommand command)
    {
        var pGroups = sourceDbPlayers
            .GroupBy(keyFuncs[depth])
            .Select(x => (x.Key, x.ToList()))
            .ToList();
        foreach (var (pKey, pList) in pGroups)
        {
            if (pList.Count == 1)
            {
                var playersFromSaves = new Dictionary<int, Player>(saveFilesPlayers.Count);
                for (var fileId = 1; fileId <= saveFilesPlayers.Count; fileId++)
                {
                    var matchingSavePlayers = saveFilesPlayers[fileId]
                        .Where(x => keyFuncs[depth].Equals(pKey))
                        .ToList();

                    if (matchingSavePlayers.Count > 1)
                    {
                        var matchingSavePlayer = matchingSavePlayers
                            .OrderByDescending(x => x.ActualYearOfBirth == pList[0].ActualYearOfBirth)
                            .ThenByDescending(x => x.DateOfBirth == pList[0].DateOfBirth)
                            .ThenByDescending(x => x.ClubId == pList[0].ClubId)
                            .GetMostRelevantPlayer()!;

                        saveFilesPlayers[fileId].RemoveAll(matchingSavePlayers.Contains);

                        playersFromSaves.Add(fileId, matchingSavePlayer);
                    }
                    else if (matchingSavePlayers.Count == 1)
                    {
                        saveFilesPlayers[fileId].Remove(matchingSavePlayers[0]);

                        playersFromSaves.Add(fileId, matchingSavePlayers[0]);
                    }
                }
                ImportPlayer(pList[0], playersFromSaves, collectedDbIdMap, saveFilesPlayers.Count, command);
            }
            else
            {
                var nextDepth = depth + 1;
                if (nextDepth == keyFuncs.Count)
                {
                    throw new NotSupportedException("There are no further keys available to distinguish players.");
                }
                else
                {
                    CrawlPlayers(pList, nextDepth, keyFuncs, saveFilesPlayers, collectedDbIdMap, command);
                }
            }
        }
    }

    private static void RebindPlayerIds(Player player,
        int fileId,
        Dictionary<(int Key, int Value), int> nationsMapRebind,
        Dictionary<(int Key, int Value), int> clubsMapRebind)
    {
        player.ClubId = GetRebindMapDbId(clubsMapRebind, fileId, player.ClubId);
        player.NationId = GetRebindMapDbId(nationsMapRebind, fileId, player.NationId);
        player.SecondaryNationId = GetRebindMapDbId(nationsMapRebind, fileId, player.SecondaryNationId);
    }

    private static void ImportPlayer(Player dbPlayer,
        Dictionary<int, Player> savesPlayer,
        List<SaveIdMapper> collectedDbIdMap,
        int savesCount,
        MySqlCommand command)
    {
        if (savesPlayer.Count / (decimal)savesCount < Settings.MinPlayerOccurencesRate)
        {
            return;
        }

        if (DateTime.Now.Year == 2025)
        {
            Console.WriteLine("Player imported");
            return;
        }

        var savesValues = savesPlayer.Select(x => x.Value).ToList();

        byte GetSrcOrMax(Func<Player, byte> getFunc)
            => getFunc(dbPlayer) > 0 ? getFunc(dbPlayer) : savesValues.GetMaxOccurence(getFunc).Key;

        int GetSrcOrAvg(Func<Player, int> getFunc)
            => getFunc(dbPlayer) > 0 ? getFunc(dbPlayer) : Avg(getFunc);

        int Avg(Func<Player, int> getFunc)
            => (int)Math.Round(savesValues.Average(getFunc));

        var clubId = dbPlayer.ClubId;
        var clubIdSaves = savesValues.GetMaxOccurence(x => x.ClubId).Key;
        if (clubId != clubIdSaves)
        {
            System.Diagnostics.Debug.WriteLine("Save files have a different club than source database for the player.");
            clubId = clubIdSaves;
        }

        DateTime? endOfContract = null;
        if (clubId != -1)
        {
            if (dbPlayer.Contract != null && dbPlayer.Contract.ContractEndDate.HasValue && dbPlayer.Contract.ContractEndDate.Value.Year != 1900)
            {
                endOfContract = dbPlayer.Contract.ContractEndDate.Value;
            }
            else if (savesValues.Count(x => (x.Contract?.ContractEndDate).HasValue) / (decimal)savesCount > Settings.MinValueOccurenceRate)
            {
                endOfContract = savesValues.Where(x => (x.Contract?.ContractEndDate).HasValue).Average(x => x.Contract!.ContractEndDate!.Value);
            }
        }

        var fields = new Dictionary<string, object>
        {
            { "first_name", dbPlayer.FirstName.DbNullIf(string.Empty) },
            { "last_name", dbPlayer.LastName.DbNullIf(string.Empty) },
            { "common_name", dbPlayer.CommonName.DbNullIf(string.Empty) },
            { "date_of_birth", dbPlayer.ActualDateOfBirth ?? savesValues.Average(x => x.DateOfBirth) },
            { "right_foot", GetSrcOrAvg(x => x.RightFoot) },
            { "left_foot", GetSrcOrAvg(x => x.LeftFoot) },
            { "nation_id", dbPlayer.NationId.DbNullIf(-1) },
            { "secondary_nation_id", dbPlayer.SecondaryNationId.DbNullIf(-1) },
            { "caps", dbPlayer.InternationalCaps },
            { "international_goals", dbPlayer.InternationalGoals },
            { "ability", GetSrcOrAvg(x => x.CurrentAbility) },
            { "potential_ability", GetSrcOrAvg(x => x.PotentialAbility) },
            { "home_reputation", GetSrcOrAvg(x => x.HomeReputation) },
            { "current_reputation", GetSrcOrAvg(x => x.CurrentAbility) },
            { "world_reputation", GetSrcOrAvg(x => x.WorldReputation) },
            // contract
            { "club_id", clubId.DbNullIf(-1) },
            { "value", clubId == -1 ? 0 : GetSrcOrAvg(x => x.Value) },
            { "contract_expiration", endOfContract.DbNullIf() },
            { "wage", clubId == -1 ? 0 : GetSrcOrAvg(x => x.Wage) },
            { "manager_job_rel", clubId == -1 ? 0 : Avg(x => (x.Contract?.ManagerReleaseClause ?? false) ? x.Contract.ReleaseClauseValue : 0) },
            { "min_fee_rel", clubId == -1 ? 0 : Avg(x => (x.Contract?.MinimumFeeReleaseClause ?? false) ? x.Contract.ReleaseClauseValue : 0) },
            { "non_play_rel", clubId == -1 ? 0 : Avg(x => (x.Contract?.NonPlayingReleaseClause ?? false) ? x.Contract.ReleaseClauseValue : 0) },
            { "non_promotion_rel", clubId == -1 ? 0 : Avg(x => (x.Contract?.NonPromotionReleaseClause ?? false) ? x.Contract.ReleaseClauseValue : 0) },
            { "relegation_rel", clubId == -1 ? 0 : Avg(x => (x.Contract?.RelegationReleaseClause ?? false) ? x.Contract.ReleaseClauseValue : 0) },
            { "transfer_status", clubId == -1 ? DBNull.Value : ((int?)savesValues.GetMaxOccurence(x => x.Contract?.TransferStatus).Key).DbNullIf() },
            { "squad_status", clubId == -1 ? DBNull.Value : ((int?)savesValues.GetMaxOccurence(x => x.Contract?.SquadStatus).Key).DbNullIf() },
            // positioning
            { "pos_goalkeeper", GetSrcOrMax(x => x.GoalKeeperPos)  },
            { "pos_sweeper", GetSrcOrMax(x => x.SweeperPos) },
            { "pos_defender", GetSrcOrMax(x => x.DefenderPos) },
            { "pos_defensive_midfielder", GetSrcOrMax(x => x.DefensiveMidfielderPos) },
            { "pos_midfielder", GetSrcOrMax(x => x.MidfielderPos) },
            { "pos_attacking_midfielder", GetSrcOrMax(x => x.AttackingMidfielderPos) },
            { "pos_forward", GetSrcOrMax(x => x.StrikerPos) },
            { "pos_wingback", GetSrcOrMax(x => x.WingBackPos) },
            { "pos_free_role", GetSrcOrMax(x => x.FreeRolePos) },
            { "side_left", GetSrcOrMax(x => x.LeftSide) },
            { "side_right", GetSrcOrMax(x => x.RightSide) },
            { "side_center", GetSrcOrMax(x => x.CenterSide) },
            // attributes non intrinsic
            { "acceleration", 0 },
            { "adaptability", 0 },
            { "aggression", 0 },
            { "agility", 0 },
            { "ambition", 0 },
            { "balance", 0 },
            { "bravery", 0 },
            { "consistency", 0 },
            { "corners", 0 },
            { "determination", 0 },
            { "dirtiness", 0 },
            { "flair", 0 },
            { "important_matches", 0 },
            { "influence", 0 },
            { "injury_proneness", 0 },
            { "jumping", 0 },
            { "loyalty", 0 },
            { "natural_fitness", 0 },
            { "pace", 0 },
            { "pressure", 0 },
            { "professionalism", 0 },
            { "set_pieces", 0 },
            { "sportsmanship", 0 },
            { "stamina", 0 },
            { "strength", 0 },
            { "teamwork", 0 },
            { "technique", 0 },
            { "temperament", 0 },
            { "versatility", 0 },
            { "work_rate", 0 },
            // intrinstic: base
            { "anticipation", 0 },
            { "creativity", 0 },
            { "crossing", 0 },
            { "decisions", 0 },
            { "dribbling", 0 },
            { "finishing", 0 },
            { "handling", 0 },
            { "heading", 0 },
            { "long_shots", 0 },
            { "marking", 0 },
            { "off_the_ball", 0 },
            { "one_on_ones", 0 },
            { "passing", 0 },
            { "positioning", 0 },
            { "reflexes", 0 },
            { "tackling", 0 },
            { "penalties", 0 },
            { "throw_ins", 0 },
            // intrinstic: potential
            { "anticipation_potential", 0 },
            { "creativity_potential", 0 },
            { "crossing_potential", 0 },
            { "decisions_potential", 0 },
            { "dribbling_potential", 0 },
            { "finishing_potential", 0 },
            { "handling_potential", 0 },
            { "heading_potential", 0 },
            { "long_shots_potential", 0 },
            { "marking_potential", 0 },
            { "off_the_ball_potential", 0 },
            { "one_on_ones_potential", 0 },
            { "passing_potential", 0 },
            { "positioning_potential", 0 },
            { "reflexes_potential", 0 },
            { "tacking_potential", 0 },
            { "penalties_potential", 0 },
            { "throw_ins_potential", 0 }
        };

        foreach (var f in fields.Keys)
        {
            command.Parameters[$"@{f}"].Value = fields[f];
        }
        command.ExecuteNonQuery();

        var map = new SaveIdMapper
        {
            DbId = (int)command.LastInsertedId,
            SaveId = savesPlayer.ToDictionary(x => x.Key, x => x.Value.Id)
        };

        collectedDbIdMap.Add(map);
    }

    #region reusable

    private List<SaveIdMapper> ImportData<T>(
        Func<BaseFileData, Dictionary<int, T>> sourceDataGet,
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

        var dataList = new List<BaseFileData>(saveFilePaths.Length + 1)
        {
            GetDbFileDataFromCache()
        };
        foreach (var saveFilePath in saveFilePaths)
        {
            dataList.Add(GetSaveGameDataFromCache(saveFilePath));
        }

        var iFile = 0;
        foreach (var data in dataList)
        {
            var sourceData = sourceDataGet(data);

            foreach (var key in sourceData.Keys)
            {
                var functionnalKey = buildKey(sourceData[key], iFile);

                var match = mapping.FirstOrDefault(x => x.Key.Equals(functionnalKey, StringComparison.InvariantCultureIgnoreCase));
                if (match.Equals(default(SaveIdMapper)))
                {
                    if (iFile > 0)
                    {
                        throw new InvalidOperationException($"The key '{functionnalKey}' only exists in save files (file: '{saveFilePaths[iFile - 1]}').");
                    }

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

    private BaseFileData GetDbFileDataFromCache()
    {
        if (!_loadedSaveData.TryGetValue("db_save_file", out var data))
        {
            data = DbFileHandler.GetDbFileData();
            _loadedSaveData.Add("db_save_file", data);
        }

        return data;
    }

    private BaseFileData GetSaveGameDataFromCache(string saveFilePath)
    {
        if (!_loadedSaveData.TryGetValue(saveFilePath, out var data))
        {
            data = SaveGameHandler.OpenSaveGameIntoMemory(saveFilePath);
            _loadedSaveData.Add(saveFilePath, data);
        }

        return data;
    }

    private static int GetMapDbId(List<SaveIdMapper> mapping, int fileIndex, int saveId)
    {
        if (saveId < 0)
        {
            return -1;
        }

        var match = mapping.FirstOrDefault(x => x.SaveId.TryGetValue(fileIndex, out var currentSaveId) && currentSaveId == saveId);
        return match.Equals(default(SaveIdMapper)) ? -1 : match.DbId;
    }

    private static int GetRebindMapDbId(Dictionary<(int, int), int> mapping, int fileIndex, int saveId)
        => saveId >= 0 && mapping.TryGetValue((fileIndex, saveId), out var dbId) ? dbId : -1;

    private static DbType GetDbType(string column)
    {
        return PlayerTableStringColumns.Contains(column)
            ? DbType.String
            : (PlayerTableDateColumns.Contains(column)
                ? DbType.Date
                : DbType.Int32);
    }

    #endregion
}
