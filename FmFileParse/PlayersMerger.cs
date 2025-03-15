using System.Data;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class PlayersMerger(int numberOfSaves, Action<string> reportProgress)
{
    private readonly int _numberOfSaves = numberOfSaves;
    private readonly Func<MySqlConnection> _getConnection = () => new MySqlConnection(Settings.ConnString);
    private readonly Action<string> _reportProgress = reportProgress;

    private static readonly string[] SqlColumns = ["occurences", .. Settings.CommonSqlColumns];

    public void ProceedToMerge()
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
                        CreatePlayerFromUnmergedPlayers(name, clubDobNamePlayersRd, writePlayerCmd, collectedMergeInfo);
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
                        CreatePlayerFromUnmergedPlayers(name, clubNamePlayersRd, writePlayerCmd, collectedMergeInfo);
                    }
                }
            }
            else
            {
                SetNameValuesOnCommand(playersByNameCmd, name);
                using var playersByNameRd = playersByNameCmd.ExecuteReader();
                CreatePlayerFromUnmergedPlayers(name, playersByNameRd, writePlayerCmd, collectedMergeInfo);
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
            "GROUP BY filename, club_id " +
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
            $"GROUP BY filename " +
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
        command.CommandText = "DELETE FROM players_merge_statistics";
        command.ExecuteNonQuery();
        command.CommandText = "DELETE FROM players";
        command.ExecuteNonQuery();
        command.CommandText = $"ALTER TABLE players AUTO_INCREMENT = 1";
        command.ExecuteNonQuery();
    }

    private void CreatePlayerFromUnmergedPlayers(
        PlayerName playerName,
        MySqlDataReader playerInfoReader, 
        MySqlCommand insertPlayerCommand,
        Dictionary<int, List<(string field, int occurences, MergeType mergeType)>> collectedMergeInfo)
    {
        var allFilePlayerData = new List<Dictionary<string, object>>(_numberOfSaves);
        while (playerInfoReader.Read())
        {
            var singleFilePlayerData = new Dictionary<string, object>(playerInfoReader.FieldCount);
            for (var i = 0; i < playerInfoReader.FieldCount; i++)
            {
                if (!Settings.UnmergedOnlyColumns.Contains(playerInfoReader.GetName(i)))
                {
                    singleFilePlayerData.Add(playerInfoReader.GetName(i), playerInfoReader[i]);
                }
            }
            allFilePlayerData.Add(singleFilePlayerData);
        }

        // there's not enough data across all files for the player
        if (allFilePlayerData.Count / (decimal)_numberOfSaves < Settings.MinPlayerOccurencesRate)
        {
            _reportProgress($"The player '{playerName}' has not enough data to be merged.");
            return;
        }

        var colsStats = new List<(string field, int distinctOccurences, MergeType mergeType)>(SqlColumns.Length);
        var colsAndVals = new Dictionary<string, object>(SqlColumns.Length);
        foreach (var col in allFilePlayerData[0].Keys)
        {
            MergeType mergeType;
            int countValues;
            object computedValue;
            if (Settings.DateColumns.Contains(col))
            {
                (computedValue, mergeType, countValues) = CrawlColumnValuesForMerge<DateTime>(allFilePlayerData, col, x => x.Average());
            }
            else if (Settings.StringColumns.Contains(col))
            {
                (computedValue, mergeType, countValues) = CrawlColumnValuesForMerge<string>(allFilePlayerData, col, null);
            }
            else if (Settings.ForeignKeyColumns.Contains(col))
            {
                (computedValue, mergeType, countValues) = CrawlColumnValuesForMerge<int>(allFilePlayerData, col, null);
            }
            else
            {
                (computedValue, mergeType, countValues) = CrawlColumnValuesForMerge<int>(allFilePlayerData, col, x => (int)Math.Round(x.Average()));
            }

            colsAndVals.Add(col, computedValue);
            colsStats.Add((col, countValues, mergeType));
        }

        colsAndVals.Add("occurences", allFilePlayerData.Count);

        foreach (var c in colsAndVals.Keys)
        {
            insertPlayerCommand.Parameters[$"@{c}"].Value = colsAndVals[c];
        }
        insertPlayerCommand.ExecuteNonQuery();

        collectedMergeInfo.Add((int)insertPlayerCommand.LastInsertedId, colsStats);

        _reportProgress($"The player '{playerName}' has been merged.");
    }

    private static (object, MergeType, int) CrawlColumnValuesForMerge<T>(
        List<Dictionary<string, object>> allFilePlayerData,
        string columnName,
        Func<IEnumerable<T>, T>? averageFunc)
    {
        var counter = new Dictionary<DbType<T>, int>(allFilePlayerData.Count);
        DbType<T> currentMax = default;
        var currentMaxCount = 0;
        var collectedInts = new List<T>(allFilePlayerData.Count);
        foreach (var singleValue in allFilePlayerData.Select(_ => _[columnName]))
        {
            var dbVal = new DbType<T>(singleValue);
            if (counter.TryGetValue(dbVal, out var value))
            {
                counter[dbVal] = value + 1;
            }
            else
            {
                counter.Add(dbVal, 1);
            }

            if (currentMaxCount < counter[dbVal])
            {
                currentMax = dbVal;
                currentMaxCount = counter[dbVal];
            }

            if (!dbVal.IsDbNull)
            {
                collectedInts.Add(dbVal.Value);
            }
        }

        var aboveThreshold = currentMaxCount / (decimal)allFilePlayerData.Count >= Settings.MinValueOccurenceRate;

        MergeType mergeType;
        object computedValue;
        if (averageFunc is not null && !aboveThreshold && !counter.ContainsKey(DbType<T>.DbNull))
        {
            computedValue = averageFunc(collectedInts)!;
            mergeType = MergeType.Average;
        }
        else
        {
            computedValue = currentMax.GetValue();
            mergeType = aboveThreshold ? MergeType.ModeAboveThreshold : MergeType.ModeBelowThreshold;
        }

        return (computedValue, mergeType, counter.Keys.Count);
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
