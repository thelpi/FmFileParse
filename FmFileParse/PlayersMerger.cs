using System.Data;
using FmFileParse.Models;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class PlayersMerger(int numberOfSaves, Action<string> reportProgress)
{
    private readonly int _numberOfSaves = numberOfSaves;
    private readonly Func<MySqlConnection> _getConnection = () => new MySqlConnection(Settings.ConnString);
    private readonly Action<string> _reportProgress = reportProgress;

    private static readonly string[] SqlColumns = ["occurences", .. Settings.CommonSqlColumns];

    public void ProceedToMerge(DataImporter dataImporter, string[] saveFilePaths)
    {
        RemoveDataFromPlayersTable();

        var writePlayerCmd = PrepareInsertPlayer();
        var playersByNameCmd = PrepareGetPlayersByName();
        var playersCountCmd = PrepareGetPlayersCountForNameByFile();
        var distinctPlayersCmd = PrepareGetDistinctPlayerNames();
        var playersCountClubCmd = PrepareGetPlayersCountForNameByClubAndFile();
        var playersByClubCmd = PrepareGetPlayersForNameByClub();
        var playersByClubDobCmd = PrepareGetPlayersForNameByClubAndDob();
        var clubNamePlayersCmd = PrepareGetPlayersForNameAndClub();
        var clubDobNamePlayersCmd = PrepareGetPlayersForNameAndClubAndDob();

        SetForeignKeysCheck(false);

        var collectedMergeInfo = new Dictionary<int, List<(string field, int occurences, MergeType mergeType)>>(520);
        var collectedDbIdMap = new List<SaveIdMapper>(10000);

        using var namesReader = distinctPlayersCmd.ExecuteReader();
        while (namesReader.Read())
        {
            var name = new PlayerName
            {
                FirstName = namesReader.IsDBNull("first_name") ? null : namesReader.GetString("first_name"),
                LastName = namesReader.IsDBNull("last_name") ? null : namesReader.GetString("last_name"),
                CommonName = namesReader.IsDBNull("common_name") ? null : namesReader.GetString("common_name")
            };

            SetNameValuesOnCommand(playersCountCmd, name);
            if (Convert.ToInt32(playersCountCmd.ExecuteScalar()) > 1)
            {
                SetNameValuesOnCommand(playersCountClubCmd, name);
                if (Convert.ToInt32(playersCountClubCmd.ExecuteScalar()) > 1)
                {
                    SetNameValuesOnCommand(playersByClubDobCmd, name);
                    using var playersByClubDobRd = playersByClubDobCmd.ExecuteReader();
                    while (playersByClubDobRd.Read())
                    {
                        SetNameValuesOnCommand(clubDobNamePlayersCmd, name);
                        clubDobNamePlayersCmd.Parameters["@club_id"].Value = playersByClubDobRd["club_id"];
                        clubDobNamePlayersCmd.Parameters["@date_of_birth"].Value = playersByClubDobRd["date_of_birth"];
                        using var clubDobNamePlayersRd = clubDobNamePlayersCmd.ExecuteReader();
                        var mapId = CreatePlayerFromUnmergedPlayers(name, clubDobNamePlayersRd, writePlayerCmd, collectedMergeInfo);
                        if (mapId.HasValue)
                        {
                            collectedDbIdMap.Add(mapId.Value);
                        }
                    }
                }
                else
                {
                    SetNameValuesOnCommand(playersByClubCmd, name);
                    using var playersByClubRd = playersByClubCmd.ExecuteReader();
                    while (playersByClubRd.Read())
                    {
                        SetNameValuesOnCommand(clubNamePlayersCmd, name);
                        clubNamePlayersCmd.Parameters["@club_id"].Value = playersByClubRd["club_id"];
                        using var clubNamePlayersRd = clubNamePlayersCmd.ExecuteReader();
                        var mapId = CreatePlayerFromUnmergedPlayers(name, clubNamePlayersRd, writePlayerCmd, collectedMergeInfo);
                        if (mapId.HasValue)
                        {
                            collectedDbIdMap.Add(mapId.Value);
                        }
                    }
                }
            }
            else
            {
                SetNameValuesOnCommand(playersByNameCmd, name);
                using var playersByNameRd = playersByNameCmd.ExecuteReader();
                var mapId = CreatePlayerFromUnmergedPlayers(name, playersByNameRd, writePlayerCmd, collectedMergeInfo);
                if (mapId.HasValue)
                {
                    collectedDbIdMap.Add(mapId.Value);
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

        clubDobNamePlayersCmd.Finalize();
        clubNamePlayersCmd.Finalize();
        playersByClubDobCmd.Finalize();
        playersByClubCmd.Finalize();
        playersCountClubCmd.Finalize();
        distinctPlayersCmd.Finalize();
        playersCountCmd.Finalize();
        playersByNameCmd.Finalize();
        writePlayerCmd.Finalize();

        _reportProgress("Saves savefiles references map...");

        dataImporter.SetSaveFileReferences(collectedDbIdMap, nameof(Player));

        _reportProgress("Updates club's staff information...");

        dataImporter.UpdateStaffOnClubs(collectedDbIdMap, saveFilePaths);
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

    private static void SetNameValuesOnCommand(MySqlCommand cdCmd, PlayerName name)
    {
        cdCmd.Parameters["@first_name"].Value = name.FirstName is null ? DBNull.Value : name.FirstName;
        cdCmd.Parameters["@last_name"].Value = name.LastName is null ? DBNull.Value : name.LastName;
        cdCmd.Parameters["@common_name"].Value = name.CommonName is null ? DBNull.Value : name.CommonName;
    }

    private MySqlCommand PrepareGetPlayersForNameAndClubAndDob()
    {
        var connection = _getConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * " +
            "FROM unmerged_players " +
            $"WHERE {GetPlayerNameSqlEquality()} " +
            $"AND {"club_id".GetSqlEqualNull()} " +
            "AND date_of_birth = @date_of_birth";
        command.SetParameter("first_name", DbType.String);
        command.SetParameter("last_name", DbType.String);
        command.SetParameter("common_name", DbType.String);
        command.SetParameter("club_id", DbType.Int32);
        command.SetParameter("date_of_birth", DbType.Date);
        command.Prepare();
        return command;
    }

    private MySqlCommand PrepareGetPlayersForNameAndClub()
    {
        var connection = _getConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * " +
            "FROM unmerged_players " +
            $"WHERE {GetPlayerNameSqlEquality()} " +
            $"AND {"club_id".GetSqlEqualNull()}";
        command.SetParameter("first_name", DbType.String);
        command.SetParameter("last_name", DbType.String);
        command.SetParameter("common_name", DbType.String);
        command.SetParameter("club_id", DbType.Int32);
        command.Prepare();
        return command;
    }

    private MySqlCommand PrepareGetPlayersForNameByClubAndDob()
    {
        var connection = _getConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT first_name, last_name, common_name, club_id, date_of_birth " +
            "FROM unmerged_players " +
            $"WHERE {GetPlayerNameSqlEquality()} " +
            "GROUP BY club_id, date_of_birth";
        command.SetParameter("first_name", DbType.String);
        command.SetParameter("last_name", DbType.String);
        command.SetParameter("common_name", DbType.String);
        command.Prepare();
        return command;
    }

    private MySqlCommand PrepareGetPlayersForNameByClub()
    {
        var connection = _getConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT first_name, last_name, common_name, club_id " +
            "FROM unmerged_players " +
            $"WHERE {GetPlayerNameSqlEquality()} " +
            "GROUP BY club_id";
        command.SetParameter("first_name", DbType.String);
        command.SetParameter("last_name", DbType.String);
        command.SetParameter("common_name", DbType.String);
        command.Prepare();
        return command;
    }

    private MySqlCommand PrepareGetPlayersCountForNameByClubAndFile()
    {
        var connection = _getConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) " +
            "FROM unmerged_players " +
            $"WHERE {GetPlayerNameSqlEquality()} " +
            "GROUP BY file_id, club_id " +
            "ORDER BY COUNT(*) DESC";
        command.SetParameter("first_name", DbType.String);
        command.SetParameter("last_name", DbType.String);
        command.SetParameter("common_name", DbType.String);
        command.Prepare();
        return command;
    }

    private MySqlCommand PrepareGetDistinctPlayerNames()
    {
        var connection = _getConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT common_name, last_name, first_name " +
            "FROM unmerged_players " +
            "ORDER BY common_name, last_name, first_name";
        command.Prepare();
        return command;
    }

    private MySqlCommand PrepareGetPlayersCountForNameByFile()
    {
        var connection = _getConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) " +
            "FROM unmerged_players " +
            $"WHERE {GetPlayerNameSqlEquality()} " +
            $"GROUP BY file_id " +
            $"ORDER BY COUNT(*) DESC";
        command.SetParameter("first_name", DbType.String);
        command.SetParameter("last_name", DbType.String);
        command.SetParameter("common_name", DbType.String);
        command.Prepare();
        return command;
    }

    private MySqlCommand PrepareGetPlayersByName()
    {
        var connection = _getConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT * " +
            "FROM unmerged_players " +
            $"WHERE {GetPlayerNameSqlEquality()}";
        command.SetParameter("first_name", DbType.String);
        command.SetParameter("last_name", DbType.String);
        command.SetParameter("common_name", DbType.String);
        command.Prepare();
        return command;
    }

    private MySqlCommand PrepareInsertPlayer()
    {
        var connection = _getConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = SqlColumns.GetInsertQuery("players");
        foreach (var c in SqlColumns)
        {
            command.SetParameter(c, Settings.GetDbType(c));
        }
        command.Prepare();
        return command;
    }

    private void RemoveDataFromPlayersTable()
    {
        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM save_files_references WHERE data_type = @data_type";
        command.SetParameter("@data_type", DbType.String, nameof(Player));
        command.ExecuteNonQuery();

        command.Parameters.Clear();
        command.CommandText = "TRUNCATE TABLE players_merge_statistics";
        command.ExecuteNonQuery();

        command.CommandText = "DELETE FROM players";
        command.ExecuteNonQuery();

        command.CommandText = $"ALTER TABLE players AUTO_INCREMENT = 1";
        command.ExecuteNonQuery();
    }

    private SaveIdMapper? CreatePlayerFromUnmergedPlayers(
        PlayerName playerName,
        MySqlDataReader playerInfoReader, 
        MySqlCommand insertPlayerCommand,
        Dictionary<int, List<(string field, int occurences, MergeType mergeType)>> collectedMergeInfo)
    {
        var allFilePlayerData = new List<Dictionary<string, object>>(_numberOfSaves);
        var collectedSaveIds = new Dictionary<int, int>();
        while (playerInfoReader.Read())
        {
            var singleFilePlayerData = new Dictionary<string, object>(playerInfoReader.FieldCount);
            var saveId = -1;
            var fileId = -1;
            for (var i = 0; i < playerInfoReader.FieldCount; i++)
            {
                var colName = playerInfoReader.GetName(i);
                if (!Settings.UnmergedOnlyColumns.Contains(colName))
                {
                    singleFilePlayerData.Add(colName, playerInfoReader[i]);
                }
                else if (colName == "id")
                {
                    saveId = playerInfoReader.GetInt32(i);
                }
                else if (colName == "file_id")
                {
                    fileId = playerInfoReader.GetInt32(i);
                }
            }
            allFilePlayerData.Add(singleFilePlayerData);
            collectedSaveIds.Add(fileId, saveId);
        }

        // there's not enough data across all files for the player
        if (allFilePlayerData.Count / (decimal)_numberOfSaves < Settings.MinPlayerOccurencesRate)
        {
            _reportProgress($"The player '{playerName}' has not enough data to be merged.");
            return null;
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

        _reportProgress($"The player '{playerName}' has been merged.");

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

    private static string GetPlayerNameSqlEquality()
        => $"{"first_name".GetSqlEqualNull()} AND {"last_name".GetSqlEqualNull()} AND {"common_name".GetSqlEqualNull()}";

    private readonly struct PlayerName
    {
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? CommonName { get; init; }

        public override string ToString()
        {
            return CommonName is not null
                ? CommonName
                : string.Concat(LastName, ", ", FirstName);
        }
    }
}
