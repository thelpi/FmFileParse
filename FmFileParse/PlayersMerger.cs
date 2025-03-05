using System.Data;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class PlayersMerger(string connectionString, int numberOfSaves, Action<(string, bool)> sendPlayerCreationReport)
{
    private const decimal _useCurrentValueRate = 2 / 3M;
    private const decimal _minimalFrequenceRate = 1 / 3M;
    private const string _occurencesColumn = "occurences";
    private const string _playersTable = "players";
    private const string _unmergedPlayersTable = "unmerged_players";

    private readonly int _numberOfSaves = numberOfSaves;
    private readonly Func<MySqlConnection> _getConnection = () => new MySqlConnection(connectionString);
    private readonly Action<(string playerName, bool isCreated)> _sendPlayerCreationReport = sendPlayerCreationReport;

    private static readonly IReadOnlyCollection<string> PlayersTableColumns =
    [
        // technical
        _occurencesColumn,
        // intrinsic
        "first_name", "last_name", "common_name", "date_of_birth", "right_foot", "left_foot",
        // country related
        "country_id", "secondary_country_id", "caps", "international_goals",
        // potential & reputation
        "ability", "potential_ability", "home_reputation", "current_reputation", "world_reputation",
        // club related
        "club_id", "value", "contract_expiration", "contract_type", "wage", "transfer_status", "squad_status",
        // release fee
        "manager_job_rel", "min_fee_rel", "non_play_rel", "non_promotion_rel", "relegation_rel",
        // positions
        "pos_goalkeeper", "pos_sweeper", "pos_defender", "pos_defensive_midfielder", "pos_midfielder",
        "pos_attacking_midfielder", "pos_forward", "pos_wingback", "pos_free_role",
        // sides
        "side_left", "side_right", "side_center",
        // attributes
        "acceleration", "adaptability", "aggression", "agility", "ambition", "anticipation",
        "balance", "bravery", "consistency", "corners", "creativity", "crossing",
        "decisions", "determination", "dirtiness", "dribbling", "finishing", "flair",
        "handling", "heading", "important_matches", "influence", "injury_proneness", "jumping",
        "long_shots", "loyalty", "marking", "natural_fitness", "off_the_ball", "one_on_ones",
        "pace", "passing", "penalties", "positioning", "pressure", "professionalism", "reflexes",
        "set_pieces", "sportsmanship", "stamina", "strength",
        "tackling", "teamwork", "technique", "temperament", "throw_ins", "versatility", "work_rate"
    ];

    // columns from 'unmerged_players' not imported in 'players'
    private static readonly IReadOnlyCollection<string> IgnoredFromSourceColumns =
    [
        "id", "filename"
    ];

    private static readonly IReadOnlyCollection<string> StringColumns =
    [
        "first_name", "last_name", "common_name", "contract_type", "transfer_status", "squad_status"
    ];

    private static readonly IReadOnlyCollection<string> DateColumns =
    [
        "date_of_birth", "contract_expiration"
    ];

    public void ProceedToMerge(bool resetAllData)
    {
        if (resetAllData)
        {
            RemoveDataFromPlayersTable();
        }

        var writePlayerCmd = PrepareInsertPlayerCommand();
        var playersByNameCmd = PrepareGetPlayersByName();
        var playersCountCmd = PrepareGetPlayersCountForNameByFile();
        var distinctPlayersCmd = PrepareGetDistinctPlayerNames();
        var playersCountClubCmd = PrepareGetPlayersCountForNameByClubAndFile();
        var playersByClubCmd = PrepareGetPlayersForNameByClub();
        var playersByClubDobCmd = PrepareGetPlayersForNameByClubAndDob();
        var clubNamePlayersCmd = PrepareGetPlayersForNameAndClub();
        var clubDobNamePlayersCmd = PrepareGetPlayersForNameAndClubAndDob();

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
                        CreatePlayerFromUnmergedPlayers(name, clubDobNamePlayersRd, writePlayerCmd);
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
                        CreatePlayerFromUnmergedPlayers(name, clubNamePlayersRd, writePlayerCmd);
                    }
                }
            }
            else
            {
                SetNameValuesOnCommand(playersByNameCmd, name);
                using var playersByNameRd = playersByNameCmd.ExecuteReader();
                CreatePlayerFromUnmergedPlayers(name, playersByNameRd, writePlayerCmd);
            }
        }

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
            $"FROM {_unmergedPlayersTable} " +
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
            $"FROM {_unmergedPlayersTable} " +
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
            $"FROM {_unmergedPlayersTable} " +
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
            $"FROM {_unmergedPlayersTable} " +
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
            $"FROM {_unmergedPlayersTable} " +
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
            $"FROM {_unmergedPlayersTable} " +
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
            $"FROM {_unmergedPlayersTable} " +
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
            $"FROM {_unmergedPlayersTable} " +
            $"WHERE {GetPlayerNameSqlEquality()}";
        command.SetParameter("first_name", DbType.String);
        command.SetParameter("last_name", DbType.String);
        command.SetParameter("common_name", DbType.String);
        command.Prepare();
        return command;
    }

    private MySqlCommand PrepareInsertPlayerCommand()
    {
        var connection = _getConnection();
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = PlayersTableColumns.GetInsertQuery(_playersTable);
        foreach (var c in PlayersTableColumns)
        {
            command.SetParameter(c, GetDbType(c));
        }
        command.Prepare();
        return command;
    }

    private void RemoveDataFromPlayersTable()
    {
        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {_playersTable}";
        command.ExecuteNonQuery();
        command.CommandText = $"ALTER TABLE {_playersTable} AUTO_INCREMENT = 1";
        command.ExecuteNonQuery();
    }

    private static DbType GetDbType(string column)
    {
        return StringColumns.Contains(column)
            ? DbType.String
            : (DateColumns.Contains(column)
                ? DbType.Date
                : DbType.Int32);
    }

    private void CreatePlayerFromUnmergedPlayers(
        PlayerName playerName,
        MySqlDataReader playerInfoReader, 
        MySqlCommand insertPlayerCommand)
    {
        var allFilePlayerData = new List<Dictionary<string, object>>(_numberOfSaves);
        while (playerInfoReader.Read())
        {
            var singleFilePlayerData = new Dictionary<string, object>(playerInfoReader.FieldCount);
            for (var i = 0; i < playerInfoReader.FieldCount; i++)
            {
                if (!IgnoredFromSourceColumns.Contains(playerInfoReader.GetName(i)))
                {
                    singleFilePlayerData.Add(playerInfoReader.GetName(i), playerInfoReader[i]);
                }
            }
            allFilePlayerData.Add(singleFilePlayerData);
        }

        // there's not enough data across all files for the player
        if (allFilePlayerData.Count / (decimal)_numberOfSaves < _minimalFrequenceRate)
        {
            _sendPlayerCreationReport((playerName.ToString(), false));
            return;
        }

        var colsAndVals = new Dictionary<string, object>(PlayersTableColumns.Count + 2);
        foreach (var col in allFilePlayerData[0].Keys)
        {
            var allValues = allFilePlayerData.Select(_ => _[col]).ToList();

            var (value, occurences) = allValues.GetRepresentativeValue();

            if (occurences / (decimal)allFilePlayerData.Count >= _useCurrentValueRate)
            {
                // when there's a common value for the column
                colsAndVals.Add(col, value);
            }
            else
            {
                var neverNullValues = allValues.All(x => x != DBNull.Value);
                if (neverNullValues && int.TryParse(allValues[0].ToString(), out _))
                {
                    // all the values are integer: proceed to average
                    colsAndVals.Add(col, Convert.ToInt32(Math.Round(allValues.Select(Convert.ToInt32).Average())));
                }
                else if (neverNullValues && DateColumns.Contains(col))
                {
                    var day = allValues.Select(x => Convert.ToDateTime(x).Day).GetRepresentativeValue();
                    var month = allValues.Select(x => Convert.ToDateTime(x).Month).GetRepresentativeValue();
                    var year = allValues.Select(x => Convert.ToDateTime(x).Year).GetRepresentativeValue();

                    // all the values are date: creates a date from the max occurence of each date part
                    colsAndVals.Add(col, new DateTime(year.value, month.value, day.value));
                }
                else
                {
                    // takes the most populated value
                    colsAndVals.Add(col, value);
                }
            }
        }

        colsAndVals.Add(_occurencesColumn, allFilePlayerData.Count);

        foreach (var c in colsAndVals.Keys)
        {
            insertPlayerCommand.Parameters[$"@{c}"].Value = colsAndVals[c];
        }
        insertPlayerCommand.ExecuteNonQuery();

        _sendPlayerCreationReport((playerName.ToString(), true));
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
