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
        ("players", "club_id", "clubs", "id"),
        ("players", "future_club_id", "clubs", "id")
    ];

    private readonly Func<MySqlConnection> _getConnection = () => new MySqlConnection(Settings.ConnString);
    private readonly Dictionary<string, BaseFileData> _loadedSaveData = [];
    private readonly Action<string> _reportProgress = reportProgress;

    public void ProceedToImport(string[] saveFilePaths)
    {
        ClearAllData();

        var confederations = ImportConfederations(saveFilePaths);

        var nations = ImportNations(saveFilePaths, confederations);

        var clubCompetitions = ImportClubCompetitions(saveFilePaths, nations);

        var clubs = ImportClubs(saveFilePaths, nations, clubCompetitions);
        SetClubsInformation(clubs, saveFilePaths);

        var players = ImportPlayers(saveFilePaths, nations, clubs);
        UpdateStaffOnClubs(clubs, players, saveFilePaths);

        CreateIndexesAndForeignKeys();
    }

    private void ClearAllData()
    {
        _reportProgress("Cleaning previous data starts...");

        DropIndexesAndForeignKeys();

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        foreach (var name in Tables)
        {
            command.CommandText = $"TRUNCATE TABLE {name}";
            command.ExecuteNonQuery();

            command.CommandText = $"ALTER TABLE {name} AUTO_INCREMENT = 1";
            command.ExecuteNonQuery();
        }
    }

    private void DropIndexesAndForeignKeys()
    {
        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        foreach (var (tSource, cSource, tTarget, _) in SqlKeysInfo)
        {
            command.CommandText = $"ALTER TABLE {tSource} DROP FOREIGN KEY {string.Concat(tSource, "_", cSource, "_", tTarget)}";
            command.ExecuteNonQuerySecured();

            command.CommandText = $"ALTER TABLE {tSource} DROP INDEX {cSource}";
            command.ExecuteNonQuerySecured();
        }
    }

    private void CreateIndexesAndForeignKeys()
    {
        _reportProgress("Creates indexes...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

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
                ("name", DbType.String, (d, iFile) => d.Name),
                ("long_name", DbType.String, (d, iFile) => d.LongName),
                ("nation_id", DbType.Int32, (d, iFile) => GetMapDbId(nationsMapping, iFile, d.NationId).DbNullIf(-1)),
                ("division_id", DbType.Int32, (d, iFile) => GetMapDbId(clubCompetitionsMapping, iFile, d.DivisionId).DbNullIf(-1))
            },
            // note: the key here should be the same as the one used in 'DataFileLoaders.ManageDuplicateClubs'
            (d, iFile) => string.Concat(d.LongName, ";", GetMapDbId(nationsMapping, iFile, d.NationId)));
    }

    private void UpdateStaffOnClubs(List<SaveIdMapper> clubsMapping, List<SaveIdMapper> playersMapping, string[] saveFilePaths)
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

        // TODO: it's sub-optimal, mapping logic on save files is not required if db file has a proper staff id

        var keysByPlayer = new Dictionary<int, List<int[]>>(clubsMapping.Count);

        foreach (var cMap in clubsMapping)
        {
            foreach (var fileId in cMap.SaveId.Keys)
            {
                if (fileId == 0)
                {
                    continue;
                }

                if (!keysByPlayer.TryGetValue(cMap.DbId, out var keys))
                {
                    keys = new List<int[]>(saveFilePaths.Length + 1);
                    keysByPlayer.Add(cMap.DbId, keys);
                }

                var club = GetSaveGameDataFromCache(saveFilePaths[fileId - 1]).Clubs[cMap.SaveId[fileId]];

                keys.Add(
                [
                    GetMapDbId(playersMapping, fileId, club.LikedStaff1),
                    GetMapDbId(playersMapping, fileId, club.LikedStaff2),
                    GetMapDbId(playersMapping, fileId, club.LikedStaff3),
                    GetMapDbId(playersMapping, fileId, club.DislikedStaff1),
                    GetMapDbId(playersMapping, fileId, club.DislikedStaff2),
                    GetMapDbId(playersMapping, fileId, club.DislikedStaff3),
                ]);
            }
        }

        var dbPropGetter = new List<Func<int, int>>
        {
            dbfId => GetDbFileDataFromCache().Clubs[dbfId].LikedStaff1,
            dbfId => GetDbFileDataFromCache().Clubs[dbfId].LikedStaff2,
            dbfId => GetDbFileDataFromCache().Clubs[dbfId].LikedStaff3,
            dbfId => GetDbFileDataFromCache().Clubs[dbfId].DislikedStaff1,
            dbfId => GetDbFileDataFromCache().Clubs[dbfId].DislikedStaff2,
            dbfId => GetDbFileDataFromCache().Clubs[dbfId].DislikedStaff3,
        };

        foreach (var pid in keysByPlayer.Keys)
        {
            var maxxedOccurences = new int[6];
            for (var i = 0; i < 6; i++)
            {
                maxxedOccurences[i] = keysByPlayer[pid].GetMaxOccurence(x => x[i]).Key;
            }

            var dbStaff = dbPropGetter.Select(x => -1).ToArray();
            if (clubsMapping.First(x => x.DbId == pid).SaveId.TryGetValue(0, out var dbFileId))
            {
                for (var i = 0; i < dbPropGetter.Count; i++)
                {
                    var localStaffId = GetMapDbId(playersMapping, 0, dbPropGetter[i](dbFileId));
                    if (localStaffId >= 0)
                    {
                        dbStaff[i] = localStaffId;
                    }
                }
            }

            object GetFinalStaffDbId(int index)
                => dbStaff[0] >= 0 ? dbStaff[0] : maxxedOccurences[0].DbNullIf(-1);

            command.Parameters["@liked_staff_1"].Value = GetFinalStaffDbId(0);
            command.Parameters["@liked_staff_2"].Value = GetFinalStaffDbId(1);
            command.Parameters["@liked_staff_3"].Value = GetFinalStaffDbId(2);
            command.Parameters["@disliked_staff_1"].Value = GetFinalStaffDbId(3);
            command.Parameters["@disliked_staff_2"].Value = GetFinalStaffDbId(4);
            command.Parameters["@disliked_staff_3"].Value = GetFinalStaffDbId(5);
            command.Parameters["@id"].Value = pid;
            command.ExecuteNonQuery();
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
            var dbId = GetMapDbId(clubsMapping, fileIndex, saveId);
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
                if (fileId == 0)
                {
                    continue;
                }

                var club = GetSaveGameDataFromCache(saveFilePaths[fileId - 1])
                    .Clubs[clubIdMap.SaveId[fileId]];

                reputationList.Add(club.Reputation);
                facilitiesList.Add(club.Facilities);
                bankList.Add(club.Bank);

                AddIfMatch(rivalClub1List, club.RivalClub1, fileId);
                AddIfMatch(rivalClub2List, club.RivalClub2, fileId);
                AddIfMatch(rivalClub3List, club.RivalClub3, fileId);
            }

            var dbClubs = GetDbFileDataFromCache().Clubs;
            var dbClub = dbClubs[clubIdMap.SaveId[0]];

            command.Parameters["@id"].Value = clubIdMap.DbId;
            command.Parameters["@rival_club_1"].Value = dbClubs.TryGetValue(dbClub.RivalClub1, out var value1)
                ? value1.Id
                : MaxOrAvgOrNull(rivalClub1List, keysCount, true);
            command.Parameters["@rival_club_2"].Value = dbClubs.TryGetValue(dbClub.RivalClub2, out var value2)
                ? value2.Id
                : MaxOrAvgOrNull(rivalClub2List, keysCount, true);
            command.Parameters["@rival_club_3"].Value = dbClubs.TryGetValue(dbClub.RivalClub3, out var value3)
                ? value3.Id
                : MaxOrAvgOrNull(rivalClub3List, keysCount, true);
            command.Parameters["@bank"].Value = dbClub.Bank == 0
                ? MaxOrAvgOrNull(bankList, keysCount, false)
                : dbClub.Bank;
            command.Parameters["@facilities"].Value = dbClub.Facilities <= 0
                ? MaxOrAvgOrNull(facilitiesList, keysCount, false)
                : dbClub.Facilities;
            command.Parameters["@reputation"].Value = dbClub.Reputation <= 0
                ? MaxOrAvgOrNull(reputationList, keysCount, false)
                : dbClub.Reputation;
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

        _reportProgress("Remaps player ID...");
        for (var fileId = 0; fileId <= saveFilePaths.Length; fileId++)
        {
            var filePlayersData = fileId == 0
                ? GetDbFileDataFromCache().Players
                : GetSaveGameDataFromCache(saveFilePaths[fileId - 1]).Players;

            foreach (var p in filePlayersData)
            {
                p.ClubId = GetMapDbId(clubsMapping, fileId, p.ClubId);
                p.NationId = GetMapDbId(nationsMapping, fileId, p.NationId);
                p.SecondaryNationId = GetMapDbId(nationsMapping, fileId, p.SecondaryNationId);
                if (p.Contract is not null)
                {
                    p.Contract.FutureClubId = GetMapDbId(clubsMapping, fileId, p.Contract.FutureClubId);
                }
            }
        }

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

        var dbPlayers = GetDbFileDataFromCache().Players;

        var keySelectors = new List<Func<Player, object>>
        {
            x => (x.CommonName, x.LastName, x.FirstName),
            x => (x.CommonName, x.LastName, x.FirstName, x.ActualYearOfBirth),
            x => (x.CommonName, x.LastName, x.FirstName, x.ActualYearOfBirth, x.DateOfBirth),
            x => (x.CommonName, x.LastName, x.FirstName, x.ActualYearOfBirth, x.DateOfBirth, x.ClubId)
        };

        CollectAndImportPlayers(dbPlayers, keySelectors, saveFilePaths, collectedDbIdMap, 0, command, null);

        return collectedDbIdMap;
    }

    private void CollectAndImportPlayers(List<Player> players,
        List<Func<Player, object>> keySelectors,
        string[] saveFilePaths,
        List<SaveIdMapper> collectedDbIdMap,
        int depth,
        MySqlCommand command,
        object? formerKey)
    {
        var pGroups = players
            .Where(x => depth == 0 || keySelectors[depth - 1](x).Equals(formerKey))
            .GroupBy(keySelectors[depth])
            .ToList();
        foreach (var pGroup in pGroups)
        {
            if (pGroup.Count() == 1)
            {
                var dbP = pGroup.First();
                var playersFromSaves = new Dictionary<int, Player>(saveFilePaths.Length);
                for (var fileId = 1; fileId <= saveFilePaths.Length; fileId++)
                {
                    var matchingSavePlayer = GetSaveGameDataFromCache(saveFilePaths[fileId - 1]).Players
                        .Where(x => keySelectors[depth](x).Equals(pGroup.Key))
                        .OrderByDescending(x => x.ClubId == dbP.ClubId)
                        .ThenByDescending(x => x.DateOfBirth == dbP.DateOfBirth)
                        .ThenByDescending(x => x.ActualYearOfBirth == dbP.ActualYearOfBirth)
                        .GetMostRelevantPlayer();
                    if (matchingSavePlayer is not null)
                    {
                        playersFromSaves.Add(fileId, matchingSavePlayer);
                    }
                }
                ImportPlayer(dbP, playersFromSaves, collectedDbIdMap, saveFilePaths.Length, command);
            }
            else
            {
                var newDepth = depth + 1;
                if (newDepth == keySelectors.Count)
                {
                    throw new NotSupportedException("There are no further keys available to distinguish players.");
                }
                else
                {
                    CollectAndImportPlayers(players, keySelectors, saveFilePaths, collectedDbIdMap, newDepth, command, pGroup.Key);
                }
            }
        }
    }

    private void ImportPlayer(Player dbPlayer,
        Dictionary<int, Player> savesPlayer,
        List<SaveIdMapper> collectedDbIdMap,
        int savesCount,
        MySqlCommand command)
    {
        if (savesPlayer.Count / (decimal)savesCount < Settings.MinPlayerOccurencesRate)
        {
            return;
        }

        // do stuff

        var map = new SaveIdMapper();

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
