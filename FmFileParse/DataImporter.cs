using System.Data;
using FmFileParse.Models;
using FmFileParse.Models.Internal;
using FmFileParse.SaveImport;
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
        var nations = ImportNations(saveFilePaths, confederations);
        var clubCompetitions = ImportClubCompetitions(saveFilePaths, nations);
        var clubs = ImportClubs(saveFilePaths, nations, clubCompetitions);
        SetClubsInformation(saveFilePaths, clubs);
        ImportPlayers(saveFilePaths, nations, clubs);
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
                command.Parameters["@filename"].Value = fileName;
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
                var data = GetSaveGameDataFromCache(saveFilePaths[fileId]);

                var club = data.Clubs[clubIdMap.SaveId[fileId]];

                reputationList.Add(club.Reputation);
                facilitiesList.Add(club.Facilities);
                bankList.Add(club.Bank);

                if (club.RivalClub1 >= 0)
                {
                    var match = clubsMapping.FirstOrDefault(x => x.SaveId.TryGetValue(fileId, out var currentId) && currentId == club.RivalClub1);
                    if (!match.Equals(default(SaveIdMapper)))
                    {
                        RivalClub1List.Add(match.DbId);
                    }
                }

                if (club.RivalClub2 >= 0)
                {
                    var match = clubsMapping.FirstOrDefault(x => x.SaveId.TryGetValue(fileId, out var currentId) && currentId == club.RivalClub2);
                    if (!match.Equals(default(SaveIdMapper)))
                    {
                        RivalClub2List.Add(match.DbId);
                    }
                }

                if (club.RivalClub3 >= 0)
                {
                    var match = clubsMapping.FirstOrDefault(x => x.SaveId.TryGetValue(fileId, out var currentId) && currentId == club.RivalClub3);
                    if (!match.Equals(default(SaveIdMapper)))
                    {
                        RivalClub3List.Add(match.DbId);
                    }
                }
            }

            var clubGroup1 = RivalClub1List.GroupBy(x => x).OrderByDescending(x => x.Count()).FirstOrDefault();
            var clubGroup2 = RivalClub2List.GroupBy(x => x).OrderByDescending(x => x.Count()).FirstOrDefault();
            var clubGroup3 = RivalClub3List.GroupBy(x => x).OrderByDescending(x => x.Count()).FirstOrDefault();
            var bankGroup = bankList.GroupBy(x => x).OrderByDescending(x => x.Count()).First();
            var reputationGroup = reputationList.GroupBy(x => x).OrderByDescending(x => x.Count()).First();
            var facilitiesGroup = facilitiesList.GroupBy(x => x).OrderByDescending(x => x.Count()).First();

            var dbClub1 = clubGroup1 != null && (clubGroup1.Count() >= Settings.MinValueOccurenceRate * keysCount
                || RivalClub1List.Count == keysCount)
                ? (object)clubGroup1.Key
                : DBNull.Value;

            var dbClub2 = clubGroup2 != null && (clubGroup2.Count() >= Settings.MinValueOccurenceRate * keysCount
                || RivalClub2List.Count == keysCount)
                ? (object)clubGroup2.Key
                : DBNull.Value;

            var dbClub3 = clubGroup3 != null && (clubGroup3.Count() >= Settings.MinValueOccurenceRate * keysCount
                || RivalClub3List.Count == keysCount)
                ? (object)clubGroup3.Key
                : DBNull.Value;

            var bank = bankGroup.Count() >= Settings.MinValueOccurenceRate * keysCount
                ? bankGroup.Key
                : (int)Math.Round(bankList.Average());

            var facilities = facilitiesGroup.Count() >= Settings.MinValueOccurenceRate * keysCount
                ? facilitiesGroup.Key
                : (int)Math.Round(facilitiesList.Average());

            var reputation = reputationGroup.Count() >= Settings.MinValueOccurenceRate * keysCount
                ? reputationGroup.Key
                : (int)Math.Round(reputationList.Average());

            command.Parameters["@id"].Value = clubIdMap.DbId;
            command.Parameters["@rival_club_1"].Value = dbClub1;
            command.Parameters["@rival_club_2"].Value = dbClub2;
            command.Parameters["@rival_club_3"].Value = dbClub3;
            command.Parameters["@bank"].Value = bank;
            command.Parameters["@facilities"].Value = facilities;
            command.Parameters["@reputation"].Value = reputation;
            command.ExecuteNonQuery();
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
                    if (match.SaveId.TryGetValue(iFile, out var value))
                    {
                        
                    }
                    else
                    {
                        match.SaveId.Add(iFile, sourceData[key].Id);
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
        => saveId < 0 ? -1 :mapping.First(x => x.SaveId.TryGetValue(fileIndex, out var currentSaveId) && currentSaveId == saveId).DbId;

    private static object GetCleanDbName(int nameId, Dictionary<int, string> names)
    {
        return names.TryGetValue(nameId, out var localName)
            && !string.IsNullOrWhiteSpace(localName)
            ? localName.Trim().Split(NameNewLineSeparators, StringSplitOptions.RemoveEmptyEntries).Last().Trim()
            : DBNull.Value;
    }

    private readonly struct SaveIdMapper
    {
        public string Key { get; init; }

        public int DbId { get; init; }

        public Dictionary<int, int> SaveId { get; init; }
    }
}
