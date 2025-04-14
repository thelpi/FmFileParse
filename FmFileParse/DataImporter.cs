using System.Data;
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

    // only columns valuated during insertion
    private static readonly string[] PlayerTableColumns =
    [
        // standard
        "first_name", "last_name", "common_name", "date_of_birth", "right_foot", "left_foot",
        // nation related
        "nation_id", "secondary_nation_id", "caps", "international_goals",
        // ability & reputation
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
        "passing_potential", "positioning_potential", "reflexes_potential", "tackling_potential",
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
        ("players", "liked_staff_1", "players", "id"),
        ("players", "liked_staff_2", "players", "id"),
        ("players", "liked_staff_3", "players", "id"),
        ("players", "disliked_staff_1", "players", "id"),
        ("players", "disliked_staff_2", "players", "id"),
        ("players", "disliked_staff_3", "players", "id"),
        ("players", "liked_club_1", "clubs", "id"),
        ("players", "liked_club_2", "clubs", "id"),
        ("players", "liked_club_3", "clubs", "id"),
        ("players", "disliked_club_1", "clubs", "id"),
        ("players", "disliked_club_2", "clubs", "id"),
        ("players", "disliked_club_3", "clubs", "id")
    ];

    private static readonly List<(string propertyName, string columnName)> IntrinsicAttributesMap =
    [
        (nameof(Player.Anticipation), "anticipation"),
        (nameof(Player.Creativity), "creativity"),
        (nameof(Player.Crossing), "crossing"),
        (nameof(Player.Decisions), "decisions"),
        (nameof(Player.Dribbling), "dribbling"),
        (nameof(Player.Finishing), "finishing"),
        (nameof(Player.Handling), "handling"),
        (nameof(Player.Heading), "heading"),
        (nameof(Player.LongShots), "long_shots"),
        (nameof(Player.Marking), "marking"),
        (nameof(Player.OffTheBall), "off_the_ball"),
        (nameof(Player.OneOnOnes), "one_on_ones"),
        (nameof(Player.Passing), "passing"),
        (nameof(Player.Positioning), "positioning"),
        (nameof(Player.Reflexes), "reflexes"),
        (nameof(Player.Tackling), "tackling"),
        (nameof(Player.Penalties), "penalties"),
        (nameof(Player.ThrowIns), "throw_ins")
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
        UpdatePlayersPreferences(players, clubs);

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

        var playersMapRebind = playersMapping
            .Where(x => x.SaveId.ContainsKey(0))
            .ToDictionary(x => x.SaveId[0], x => x.DbId);

        foreach (var clubMap in clubsMapping)
        {
            var dbClub = GetDbFileDataFromCache().Clubs[clubMap.SaveId[0]];

            command.Parameters["@liked_staff_1"].Value = (playersMapRebind.TryGetValue(dbClub.LikedStaff1, out var dbId1) ? dbId1 : -1).DbNullIf(-1);
            command.Parameters["@liked_staff_2"].Value = (playersMapRebind.TryGetValue(dbClub.LikedStaff2, out var dbId2) ? dbId2 : -1).DbNullIf(-1);
            command.Parameters["@liked_staff_3"].Value = (playersMapRebind.TryGetValue(dbClub.LikedStaff3, out var dbId3) ? dbId3 : -1).DbNullIf(-1);
            command.Parameters["@disliked_staff_1"].Value = (playersMapRebind.TryGetValue(dbClub.DislikedStaff1, out var dbId4) ? dbId4 : -1).DbNullIf(-1);
            command.Parameters["@disliked_staff_2"].Value = (playersMapRebind.TryGetValue(dbClub.DislikedStaff2, out var dbId5) ? dbId5 : -1).DbNullIf(-1);
            command.Parameters["@disliked_staff_3"].Value = (playersMapRebind.TryGetValue(dbClub.DislikedStaff3, out var dbId6) ? dbId6 : -1).DbNullIf(-1);
            command.Parameters["@id"].Value = clubMap.DbId;
            command.ExecuteNonQuery();
        }
    }

    private void UpdatePlayersPreferences(List<SaveIdMapper> playersMapping,
        List<SaveIdMapper> clubsMapping)
    {
        _reportProgress("Updates player's preferences information...");

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE players " +
            "SET liked_staff_1 = @liked_staff_1, liked_staff_2 = @liked_staff_2, liked_staff_3 = @liked_staff_3, " +
            "disliked_staff_1 = @disliked_staff_1, disliked_staff_2 = @disliked_staff_2, disliked_staff_3 = @disliked_staff_3, " +
            "liked_club_1 = @liked_club_1, liked_club_2 = @liked_club_2, liked_club_3 = @liked_club_3, " +
            "disliked_club_1 = @disliked_club_1, disliked_club_2 = @disliked_club_2, disliked_club_3 = @disliked_club_3 " +
            "WHERE id = @id";
        command.SetParameter("liked_staff_1", DbType.Int32);
        command.SetParameter("liked_staff_2", DbType.Int32);
        command.SetParameter("liked_staff_3", DbType.Int32);
        command.SetParameter("disliked_staff_1", DbType.Int32);
        command.SetParameter("disliked_staff_2", DbType.Int32);
        command.SetParameter("disliked_staff_3", DbType.Int32);
        command.SetParameter("liked_club_1", DbType.Int32);
        command.SetParameter("liked_club_2", DbType.Int32);
        command.SetParameter("liked_club_3", DbType.Int32);
        command.SetParameter("disliked_club_1", DbType.Int32);
        command.SetParameter("disliked_club_2", DbType.Int32);
        command.SetParameter("disliked_club_3", DbType.Int32);
        command.SetParameter("id", DbType.Int32);
        command.Prepare();

        var clubsMapRebind = clubsMapping
            .Where(x => x.SaveId.ContainsKey(0))
            .ToDictionary(x => x.SaveId[0], x => x.DbId);

        var playersMapRebind = playersMapping
            .Where(x => x.SaveId.ContainsKey(0))
            .ToDictionary(x => x.SaveId[0], x => x.DbId);

        foreach (var pMap in playersMapping)
        {
            var dbPlayer = GetDbFileDataFromCache().Players.First(x => x.Id == pMap.SaveId[0]);

            command.Parameters["@liked_staff_1"].Value = (playersMapRebind.TryGetValue(dbPlayer.FavStaff1, out var dbpId1) ? dbpId1 : -1).DbNullIf(-1);
            command.Parameters["@liked_staff_2"].Value = (playersMapRebind.TryGetValue(dbPlayer.FavStaff2, out var dbpId2) ? dbpId2 : -1).DbNullIf(-1);
            command.Parameters["@liked_staff_3"].Value = (playersMapRebind.TryGetValue(dbPlayer.FavStaff3, out var dbpId3) ? dbpId3 : -1).DbNullIf(-1);
            command.Parameters["@disliked_staff_1"].Value = (playersMapRebind.TryGetValue(dbPlayer.DislikeStaff1, out var dbpId4) ? dbpId4 : -1).DbNullIf(-1);
            command.Parameters["@disliked_staff_2"].Value = (playersMapRebind.TryGetValue(dbPlayer.DislikeStaff2, out var dbpId5) ? dbpId5 : -1).DbNullIf(-1);
            command.Parameters["@disliked_staff_3"].Value = (playersMapRebind.TryGetValue(dbPlayer.DislikeStaff3, out var dbpId6) ? dbpId6 : -1).DbNullIf(-1);
            command.Parameters["@liked_club_1"].Value = (clubsMapRebind.TryGetValue(dbPlayer.FavClub1, out var dbcId1) ? dbcId1 : -1).DbNullIf(-1);
            command.Parameters["@liked_club_2"].Value = (clubsMapRebind.TryGetValue(dbPlayer.FavClub2, out var dbcId2) ? dbcId2 : -1).DbNullIf(-1);
            command.Parameters["@liked_club_3"].Value = (clubsMapRebind.TryGetValue(dbPlayer.FavClub3, out var dbcId3) ? dbcId3 : -1).DbNullIf(-1);
            command.Parameters["@disliked_club_1"].Value = (clubsMapRebind.TryGetValue(dbPlayer.DislikeClub1, out var dbcId4) ? dbcId4 : -1).DbNullIf(-1);
            command.Parameters["@disliked_club_2"].Value = (clubsMapRebind.TryGetValue(dbPlayer.DislikeClub2, out var dbcId5) ? dbcId5 : -1).DbNullIf(-1);
            command.Parameters["@disliked_club_3"].Value = (clubsMapRebind.TryGetValue(dbPlayer.DislikeClub3, out var dbcId6) ? dbcId6 : -1).DbNullIf(-1);
            command.Parameters["@id"].Value = pMap.DbId;
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

        var clubsMapRebind = clubsMapping
            .Where(x => x.SaveId.ContainsKey(0))
            .ToDictionary(x => x.SaveId[0], x => x.DbId);

        var clubCount = 0;
        foreach (var clubIdMap in clubsMapping)
        {
            var dbClub = GetDbFileDataFromCache().Clubs[clubIdMap.SaveId[0]];

            command.Parameters["@id"].Value = clubIdMap.DbId;
            command.Parameters["@rival_club_1"].Value = (clubsMapRebind.TryGetValue(dbClub.RivalClub1, out var value1) ? value1 : -1).DbNullIf(-1);
            command.Parameters["@rival_club_2"].Value = (clubsMapRebind.TryGetValue(dbClub.RivalClub2, out var value2) ? value2 : -1).DbNullIf(-1);
            command.Parameters["@rival_club_3"].Value = (clubsMapRebind.TryGetValue(dbClub.RivalClub3, out var value3) ? value3 : -1).DbNullIf(-1);
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

        var keyFuncs = new List<(Func<Player, object> keyFunc, IEqualityComparer<Player> keysComparer)>
        {
            (
                x => (x.CommonName, x.LastName, x.FirstName),
                EqualityComparer<Player>.Create((pDb, pSave) =>
                    (pDb!.CommonName, pDb.LastName, pDb.FirstName) == (pSave!.CommonName, pSave.LastName, pSave.FirstName))
            ),
            (
                x => (x.CommonName, x.LastName, x.FirstName, x.ActualYearOfBirth),
                EqualityComparer<Player>.Create((pDb, pSave) =>
                    (pDb!.CommonName, pDb.LastName, pDb.FirstName, pDb.ActualYearOfBirth) == (pSave!.CommonName, pSave.LastName, pSave.FirstName, pSave.ActualYearOfBirth))
            ),
            (
                x => (x.CommonName, x.LastName, x.FirstName, x.ActualYearOfBirth, x.DateOfBirth),
                EqualityComparer<Player>.Create((pDb, pSave) =>
                    (pDb!.CommonName, pDb.LastName, pDb.FirstName, pDb.ActualYearOfBirth, pDb.DateOfBirth) == (pSave!.CommonName, pSave.LastName, pSave.FirstName, pSave.ActualYearOfBirth, pSave.DateOfBirth))
            ),
            (
                x => (x.CommonName, x.LastName, x.FirstName, x.ActualYearOfBirth, x.DateOfBirth, x.ClubId),
                EqualityComparer<Player>.Create((pDb, pSave) =>
                    (pDb!.CommonName, pDb.LastName, pDb.FirstName, pDb.ActualYearOfBirth, pDb.DateOfBirth, pDb.ClubId) == (pSave!.CommonName, pSave.LastName, pSave.FirstName, pSave.ActualYearOfBirth, pSave.DateOfBirth, pSave.ClubId))
            )
        };

        CrawlPlayers(dbPlayers, 0, keyFuncs, saveFilesPlayers, collectedDbIdMap, command);

        return collectedDbIdMap;
    }

    private static void CrawlPlayers(List<Player> sourceDbPlayers,
        int depth,
        List<(Func<Player, object> keyFunc, IEqualityComparer<Player> keysComparer)> keyFuncs,
        Dictionary<int, List<Player>> saveFilesPlayers,
        List<SaveIdMapper> collectedDbIdMap,
        MySqlCommand command)
    {
        var pGroups = sourceDbPlayers
            .GroupBy(keyFuncs[depth].keyFunc)
            .Select(x => x.ToList())
            .ToList();
        foreach (var pList in pGroups)
        {
            if (pList.Count == 1)
            {
                var playersFromSaves = new Dictionary<int, Player>(saveFilesPlayers.Count);
                for (var fileId = 1; fileId <= saveFilesPlayers.Count; fileId++)
                {
                    var matchingSavePlayers = saveFilesPlayers[fileId]
                        .Where(x => keyFuncs[depth].keysComparer.Equals(x, pList[0]))
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

        var savesValues = savesPlayer.Select(x => x.Value).ToList();

        byte GetSrcOrMax(Func<Player, byte> getFunc)
            => getFunc(dbPlayer) > 0 ? getFunc(dbPlayer) : savesValues.GetMaxOccurence(getFunc).Key;

        int GetSrcOrMaxOrAvg(Func<Player, int> getFunc)
        {
            var sourceValue = getFunc(dbPlayer);
            if (sourceValue > 0)
            {
                return sourceValue;
            }

            var groupValues = savesValues.GroupBy(x => getFunc(x)).OrderByDescending(x => x.Count());
            return groupValues.First().Count() / (decimal)savesCount > Settings.MinValueOccurenceRate
                ? groupValues.First().Key
                : savesValues.All(x => getFunc(x) > 0)
                    ? Avg(getFunc)
                    : groupValues.First().Key;
        }

        int Avg(Func<Player, int> getFunc)
            => (int)Math.Round(savesValues.Average(getFunc));

        // TODO in case of loan, try to extract the original club from dbFile
        var clubId = dbPlayer.ClubId;
        var clubIdSaves = savesValues.GetMaxOccurence(x => x.ClubId).Key;
        var clubFromSave = clubId != clubIdSaves;
        if (clubFromSave)
        {
            System.Diagnostics.Debug.WriteLine("Save files have a different club than source database for the player.");
            clubId = clubIdSaves;
        }

        DateTime? endOfContract = null;
        if (clubId != -1)
        {
            if (!clubFromSave && (dbPlayer.Contract?.ContractEndDate).HasValue && dbPlayer.Contract!.ContractEndDate!.Value.Year != 1900)
            {
                endOfContract = dbPlayer.Contract.ContractEndDate.Value;
            }
            else
            {
                var endOfContractGroups = savesValues.GroupBy(x => x.Contract?.ContractEndDate).OrderByDescending(x => x.Count());
                endOfContract = endOfContractGroups.First().Count() / (decimal)savesCount > Settings.MinValueOccurenceRate
                    ? endOfContractGroups.First().Key
                    : savesValues.All(x => (x.Contract?.ContractEndDate).HasValue)
                        ? savesValues.Average(x => x.Contract!.ContractEndDate!.Value)
                        : endOfContractGroups.First().Key;
            }
        }

        var fields = new Dictionary<string, object>
        {
            { "first_name", dbPlayer.FirstName.DbNullIf(string.Empty) },
            { "last_name", dbPlayer.LastName.DbNullIf(string.Empty) },
            { "common_name", dbPlayer.CommonName.DbNullIf(string.Empty) },
            { "date_of_birth", dbPlayer.ActualDateOfBirth ?? savesValues.Average(x => x.DateOfBirth) },
            { "right_foot", GetSrcOrMaxOrAvg(x => x.RightFoot) },
            { "left_foot", GetSrcOrMaxOrAvg(x => x.LeftFoot) },
            { "nation_id", dbPlayer.NationId.DbNullIf(-1) },
            { "secondary_nation_id", dbPlayer.SecondaryNationId.DbNullIf(-1) },
            { "caps", dbPlayer.InternationalCaps },
            { "international_goals", dbPlayer.InternationalGoals },
            { "ability", GetSrcOrMaxOrAvg(x => x.CurrentAbility) },
            { "potential_ability", GetSrcOrMaxOrAvg(x => x.PotentialAbility) },
            { "home_reputation", GetSrcOrMaxOrAvg(x => x.HomeReputation) },
            { "current_reputation", GetSrcOrMaxOrAvg(x => x.CurrentReputation) },
            { "world_reputation", GetSrcOrMaxOrAvg(x => x.WorldReputation) },
            // contract
            { "club_id", clubId.DbNullIf(-1) },
            { "value", clubId == -1 ? 0 : GetSrcOrMaxOrAvg(x => x.Value) },
            { "contract_expiration", endOfContract.DbNullIf() },
            { "wage", clubId == -1 ? 0 : GetSrcOrMaxOrAvg(x => x.Wage) },
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
            // non-intrinsic attributes
            { "acceleration", Avg(p => p.Acceleration) },
            { "adaptability", Avg(p => p.Adaptability) },
            { "aggression", Avg(p => p.Aggression) },
            { "agility", Avg(p => p.Agility) },
            { "ambition", Avg(p => p.Ambition) },
            { "balance", Avg(p => p.Balance) },
            { "bravery", Avg(p => p.Bravery) },
            { "consistency", Avg(p => p.Consistency) },
            { "corners", Avg(p => p.Corners) },
            { "determination", Avg(p => p.Determination) },
            { "dirtiness", Avg(p => p.Dirtiness) },
            { "flair", Avg(p => p.Flair) },
            { "important_matches", Avg(p => p.ImportantMatches) },
            { "influence", Avg(p => p.Influence) },
            { "injury_proneness", Avg(p => p.InjuryProneness) },
            { "jumping", Avg(p => p.Jumping) },
            { "loyalty", Avg(p => p.Loyalty) },
            { "natural_fitness", Avg(p => p.NaturalFitness) },
            { "pace", Avg(p => p.Pace) },
            { "pressure", Avg(p => p.Pressure) },
            { "professionalism", Avg(p => p.Professionalism) },
            { "set_pieces", Avg(p => p.FreeKicks) },
            { "sportsmanship", Avg(p => p.Sportsmanship) },
            { "stamina", Avg(p => p.Stamina) },
            { "strength", Avg(p => p.Strength) },
            { "teamwork", Avg(p => p.Teamwork) },
            { "technique", Avg(p => p.Technique) },
            { "temperament", Avg(p => p.Temperament) },
            { "versatility", Avg(p => p.Versatility) },
            { "work_rate", Avg(p => p.WorkRate) }
        };

        // intrinsic attributes
        foreach (var (propertyName, columnName) in IntrinsicAttributesMap)
        {
            fields.Add(columnName, Avg(p => p.ConvertAttributeIntrinsicValue(propertyName).Item1));
            fields.Add($"{columnName}_potential", Avg(p => p.ConvertAttributeIntrinsicValue(propertyName).Item2));
        }

        foreach (var f in fields.Keys)
        {
            command.Parameters[$"@{f}"].Value = fields[f];
        }
        command.ExecuteNonQuery();

        var savesId = savesPlayer.ToDictionary(x => x.Key, x => x.Value.Id);
        savesId.Add(0, dbPlayer.Id);

        var map = new SaveIdMapper
        {
            DbId = (int)command.LastInsertedId,
            SaveId = savesId
        };

        collectedDbIdMap.Add(map);

        Console.WriteLine($"Importation of player: {dbPlayer.Fullname} ({collectedDbIdMap.Count})");
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
