using System.Data;
using System.Text;
using FmFileParse.DataClasses;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class DataImporter()
{
    private static readonly string[] SqlColumns = [.. Settings.UnmergedOnlyColumns, .. Settings.CommonSqlColumns];

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

    public void ClearAllData()
    {
        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        // note: order of deletion is important!

        command.CommandText = "DELETE FROM unmerged_players";
        command.ExecuteNonQuery();

        command.CommandText = "DELETE FROM clubs";
        command.ExecuteNonQuery();

        command.CommandText = "DELETE FROM competitions";
        command.ExecuteNonQuery();

        command.CommandText = "DELETE FROM countries";
        command.ExecuteNonQuery();
    }

    public void ImportCountries(string saveFilePath)
    {
        var data = SaveGameHandler.OpenSaveGameIntoMemory(saveFilePath);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        // TODO: confederation, is_eu
        command.CommandText = "INSERT INTO countries (id, name, is_eu, confederation_id) " +
            "VALUES (@id, @name, 0, 1)";
        command.SetParameter("id", DbType.Int32);
        command.SetParameter("name", DbType.String);
        command.Prepare();

        foreach (var key in data.Nations.Keys)
        {
            command.Parameters["@id"].Value = data.Nations[key].Id;
            command.Parameters["@name"].Value = data.Nations[key].Name;
            command.ExecuteNonQuery();
        }
    }

    public void ImportCompetitions(string saveFilePath)
    {
        var data = SaveGameHandler.OpenSaveGameIntoMemory(saveFilePath);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO competitions (id, name, long_name, acronym, country_id) " +
            "VALUES (@id, @name, @long_name, @acronym, @country_id)";
        command.SetParameter("id", DbType.Int32);
        command.SetParameter("name", DbType.String);
        command.SetParameter("long_name", DbType.String);
        command.SetParameter("acronym", DbType.String);
        command.SetParameter("country_id", DbType.Int32);
        command.Prepare();

        foreach (var key in data.ClubComps.Keys)
        {
            command.Parameters["@id"].Value = data.ClubComps[key].Id;
            command.Parameters["@name"].Value = data.ClubComps[key].Name;
            command.Parameters["@long_name"].Value = data.ClubComps[key].LongName;
            command.Parameters["@acronym"].Value = data.ClubComps[key].Abbreviation;
            command.Parameters["@country_id"].Value = data.ClubComps[key].NationId >= 0
                ? data.ClubComps[key].NationId
                : DBNull.Value;
            command.ExecuteNonQuery();
        }
    }

    public void ImportClubs(string saveFilePath)
    {
        var data = SaveGameHandler.OpenSaveGameIntoMemory(saveFilePath);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO clubs (id, name, long_name, country_id, reputation, division_id) " +
            "VALUES (@id, @name, @long_name, @country_id, @reputation, @division_id)";

        command.SetParameter("id", DbType.Int32);
        command.SetParameter("name", DbType.String);
        command.SetParameter("long_name", DbType.String);
        command.SetParameter("country_id", DbType.Int32);
        command.SetParameter("reputation", DbType.Int32);
        command.SetParameter("division_id", DbType.Int32);

        command.Prepare();

        foreach (var key in data.Clubs.Keys)
        {
            command.Parameters["@id"].Value = data.Clubs[key].ClubId;
            command.Parameters["@name"].Value = data.Clubs[key].Name;
            command.Parameters["@long_name"].Value = data.Clubs[key].LongName;
            command.Parameters["@country_id"].Value = data.Clubs[key].NationId < 0
                ? DBNull.Value
                : data.Clubs[key].NationId;
            command.Parameters["@reputation"].Value = data.Clubs[key].Reputation;
            command.Parameters["@division_id"].Value = data.Clubs[key].DivisionId >= 0
                ? data.Clubs[key].DivisionId
                : DBNull.Value;
            command.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<(string, string, Player)> ImportPlayers(
        string[] saveFilePaths,
        string[] extractFilePaths,
        Action<string> sendPlayerCreationReport)
    {
        if (saveFilePaths.Length != extractFilePaths.Length)
        {
            throw new ArgumentException("Path lists should have the same cardinal.", nameof(extractFilePaths));
        }

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = SqlColumns.GetInsertQuery("unmerged_players");

        foreach (var c in SqlColumns)
        {
            command.SetParameter(c, Settings.GetDbType(c));
        }

        command.Prepare();

        var notFoundPlayers = new List<(string, string, Player)>();

        for (var i = 0; i < saveFilePaths.Length; i++)
        {
            var fileName = Path.GetFileName(saveFilePaths[i]);

            var data = SaveGameHandler.OpenSaveGameIntoMemory(saveFilePaths[i]);

            var csvData = GetDataFromCsvFile(extractFilePaths[i]);

            foreach (var player in data.Players)
            {
                var firstName = GetCleanDbName(player._staff.FirstNameId, data.FirstNames);
                var lastName = GetCleanDbName(player._staff.SecondNameId, data.Surnames);
                var commmonName = GetCleanDbName(player._staff.CommonNameId, data.CommonNames);

                string[] keyParts =
                [
                    (commmonName == DBNull.Value ? $"{lastName}, {firstName}" : $"{commmonName}").Trim(),
                    player._player.WorldReputation.ToString(),
                    player._player.Reputation.ToString(),
                    player._player.DomesticReputation.ToString(),
                    player._player.CurrentAbility.ToString(),
                    player._player.PotentialAbility.ToString()
                ];

                if (!csvData.TryGetValue(string.Join(CsvColumnsSeparator, keyParts), out var csvPlayer))
                {
                    notFoundPlayers.Add((fileName, keyParts[0], player));
                    continue;
                }

                command.Parameters["@id"].Value = player._staff.StaffId;
                command.Parameters["@filename"].Value = fileName;
                command.Parameters["@first_name"].Value = firstName;
                command.Parameters["@last_name"].Value = lastName;
                command.Parameters["@common_name"].Value = commmonName;
                command.Parameters["@date_of_birth"].Value = player._staff.DOB;
                command.Parameters["@country_id"].Value = player._staff.NationId;
                command.Parameters["@secondary_country_id"].Value = player._staff.SecondaryNationId >= 0
                    ? player._staff.SecondaryNationId
                    : DBNull.Value;
                command.Parameters["@caps"].Value = player._staff.InternationalCaps;
                command.Parameters["@international_goals"].Value = player._staff.InternationalGoals;
                command.Parameters["@right_foot"].Value = player._player.RightFoot;
                command.Parameters["@left_foot"].Value = player._player.LeftFoot;
                command.Parameters["@ability"].Value = player._player.CurrentAbility;
                command.Parameters["@potential_ability"].Value = player._player.PotentialAbility;
                command.Parameters["@home_reputation"].Value = player._player.DomesticReputation;
                command.Parameters["@current_reputation"].Value = player._player.Reputation;
                command.Parameters["@world_reputation"].Value = player._player.WorldReputation;
                command.Parameters["@club_id"].Value = player._staff.ClubId >= 0 ? player._staff.ClubId : DBNull.Value;
                command.Parameters["@value"].Value = player._staff.Value;
                command.Parameters["@contract_expiration"].Value = player._contract?.ContractEndDate ?? (object)DBNull.Value;
                command.Parameters["@wage"].Value = player._staff.Wage;
                command.Parameters["@transfer_status"].Value = player._contract != null && Enum.IsDefined((TransferStatus)player._contract.TransferStatus)
                    ? ((TransferStatus)player._contract.TransferStatus).ToString()
                    : DBNull.Value;
                command.Parameters["@squad_status"].Value = player._contract != null && Enum.IsDefined((SquadStatus)player._contract.SquadStatus)
                    ? ((SquadStatus)player._contract.SquadStatus).ToString()
                    : DBNull.Value;
                command.Parameters["@manager_job_rel"].Value = player._contract?.ManagerReleaseClause == true
                    ? player._contract.ReleaseClauseValue
                    : 0;
                command.Parameters["@min_fee_rel"].Value = player._contract?.MinimumFeeReleaseClause == true
                    ? player._contract.ReleaseClauseValue
                    : 0;
                command.Parameters["@non_play_rel"].Value = player._contract?.NonPlayingReleaseClause == true
                    ? player._contract.ReleaseClauseValue
                    : 0;
                command.Parameters["@non_promotion_rel"].Value = player._contract?.NonPromotionReleaseClause == true
                    ? player._contract.ReleaseClauseValue
                    : 0;
                command.Parameters["@relegation_rel"].Value = player._contract?.RelegationReleaseClause == true
                    ? player._contract.ReleaseClauseValue
                    : 0;
                command.Parameters["@pos_goalkeeper"].Value = player._player.GK;
                command.Parameters["@pos_sweeper"].Value = player._player.SW;
                command.Parameters["@pos_defender"].Value = player._player.DF;
                command.Parameters["@pos_defensive_midfielder"].Value = player._player.DM;
                command.Parameters["@pos_midfielder"].Value = player._player.MF;
                command.Parameters["@pos_attacking_midfielder"].Value = player._player.AM;
                command.Parameters["@pos_forward"].Value = player._player.ST;
                command.Parameters["@pos_wingback"].Value = player._player.WingBack;
                command.Parameters["@pos_free_role"].Value = player._player.FreeRole;
                command.Parameters["@side_left"].Value = player._player.Left;
                command.Parameters["@side_right"].Value = player._player.Right;
                command.Parameters["@side_center"].Value = player._player.Centre;

                // from extract file
                foreach (var attributeName in Settings.AttributeColumns)
                {
                    command.Parameters[$"@{attributeName}"].Value = attributeName == "injury_proneness"
                        ? 20 - int.Parse(csvPlayer[OrderedCsvColumns.IndexOf("injury_proneness")])
                        : csvPlayer[OrderedCsvColumns.IndexOf(attributeName)];
                }

                command.ExecuteNonQuery();

                sendPlayerCreationReport(keyParts[0]);
            }
        }

        return notFoundPlayers;
    }

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
}
