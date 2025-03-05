using System.Data;
using System.Text;
using FmFileParse.DataClasses;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class DataImporter(string connectionString)
{
    private static readonly string[] SqlColumns =
    [
        "id", "filename", "first_name", "last_name", "common_name", "date_of_birth", "country_id", "secondary_country_id",
        "caps", "international_goals", "right_foot", "left_foot", "ability", "potential_ability", "home_reputation",
        "current_reputation", "world_reputation", "club_id", "value", "contract_expiration", "contract_type", "wage",
        "transfer_status", "squad_status", "manager_job_rel", "min_fee_rel", "non_play_rel", "non_promotion_rel", "relegation_rel",
        "pos_goalkeeper", "pos_sweeper", "pos_defender", "pos_defensive_midfielder", "pos_midfielder", "pos_attacking_midfielder",
        "pos_forward", "pos_wingback", "pos_free_role", "side_left", "side_right", "side_center", "acceleration", "adaptability",
        "aggression", "agility", "ambition", "anticipation", "balance", "bravery", "consistency", "corners", "creativity", "crossing",
        "decisions", "determination", "dirtiness", "dribbling", "finishing", "flair", "handling", "heading", "important_matches",
        "influence", "injury_proneness", "jumping", "long_shots", "loyalty", "marking", "natural_fitness", "off_the_ball", "one_on_ones",
        "pace", "passing", "penalties", "positioning", "pressure", "professionalism", "reflexes", "set_pieces", "sportsmanship",
        "stamina", "strength", "tackling", "teamwork", "technique", "temperament", "throw_ins", "versatility", "work_rate"
    ];

    private static readonly string[] OrderedCsvColumns =
    [
        "name", "nation", "club", "position", "ability", "potential_ability", "age", "value", "scout_rating",
        "acceleration", "adaptability", "aggression", "agility", "ambition", "anticipation", "balance", "bravery", "caps",
        "club_reputation", "consistency", "contract_expiration", "contract_type", "corners", "creativity", "crossing",
        "current_reputation", "date_of_birth", "decisions", "determination", "dirtiness", "dribbling", "finishing", "flair",
        "handling", "heading", "home_reputation", "important_matches", "influence", "injury_proneness", "international_goals",
        "jumping", "left_foot", "long_shots", "loyality", "manager_job_rel", "marking", "min_fee_rel", "natural_fitness",
        "non_play_rel", "non_promotion_rel", "off_the_ball", "one_on_ones", "pace", "passing", "penalties", "positioning",
        "pressure", "professionalism", "reflexes", "relegation_rel", "right_foot", "set_pieces", "sportsmanship", "squad_status",
        "stamina", "strength", "tackling", "teamwork", "technique", "temperament", "throw_ins", "transfer_status", "versatility",
        "wage", "work_rate", "world_reputation"
    ];

    private static readonly string[] StringColumns =
    [
        "filename", "first_name", "last_name", "common_name", "contract_type", "transfer_status", "squad_status"
    ];

    private static readonly string[] DateColumns =
    [
        "date_of_birth", "contract_expiration"
    ];

    private static readonly string[] CsvRowsSeparators = ["\r\n", "\r", "\n"];

    private static readonly Encoding CsvFileEncoding = Encoding.Latin1;

    private const char CsvColumnsSeparator = ';';

    private const int MaxAttributeRate = 20;

    private readonly Func<MySqlConnection> _getConnection =
        () => new MySqlConnection(connectionString);

    public void ClearAllData(bool reimportCountries)
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

        if (reimportCountries)
        {
            command.CommandText = "DELETE FROM countries";
            command.ExecuteNonQuery();
        }
    }

    public void ImportCountries(string saveFilePath)
    {
        var data = SaveGameHandler.OpenSaveGameIntoMemory(saveFilePath);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        // TODO: confederation, is_eu (when done, remove the "reimport countries" functionnality)
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
            // TODO: get proper country
            command.Parameters["@country_id"].Value = DBNull.Value; // data.ClubComps[key].NationId >= 0 ? data.ClubComps[key].NationId 
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
            command.Parameters["@country_id"].Value = data.Clubs[key].NationId < 0 ? DBNull.Value : data.Clubs[key].NationId;
            command.Parameters["@reputation"].Value = data.Clubs[key].Reputation;
            // TODO: get division
            command.Parameters["@division_id"].Value = DBNull.Value; // data.ClubComps[key].DivisionId >= 0 ? data.ClubComps[key].DivisionId
            command.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<(string, string, Player)> ImportPlayers(string[] saveFilePaths, string[] extractFilePaths)
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
            var type = StringColumns.Contains(c)
                ? DbType.String
                : (DateColumns.Contains(c)
                    ? DbType.Date
                    : DbType.Int32);
            command.SetParameter(c, type);
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

                // from save file
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

                // TODO get from source data for this two
                command.Parameters["@contract_type"].Value = string.IsNullOrWhiteSpace(csvPlayer[OrderedCsvColumns.IndexOf("contract_type")])
                    ? DBNull.Value
                    : csvPlayer[OrderedCsvColumns.IndexOf("contract_type")];
                command.Parameters["@international_goals"].Value = csvPlayer[OrderedCsvColumns.IndexOf("international_goals")];

                command.Parameters["@acceleration"].Value = csvPlayer[OrderedCsvColumns.IndexOf("acceleration")];
                command.Parameters["@adaptability"].Value = csvPlayer[OrderedCsvColumns.IndexOf("adaptability")];
                command.Parameters["@aggression"].Value = csvPlayer[OrderedCsvColumns.IndexOf("aggression")];
                command.Parameters["@agility"].Value = csvPlayer[OrderedCsvColumns.IndexOf("agility")];
                command.Parameters["@ambition"].Value = csvPlayer[OrderedCsvColumns.IndexOf("ambition")];
                command.Parameters["@anticipation"].Value = csvPlayer[OrderedCsvColumns.IndexOf("anticipation")];
                command.Parameters["@balance"].Value = csvPlayer[OrderedCsvColumns.IndexOf("balance")];
                command.Parameters["@bravery"].Value = csvPlayer[OrderedCsvColumns.IndexOf("bravery")];
                command.Parameters["@consistency"].Value = csvPlayer[OrderedCsvColumns.IndexOf("consistency")];
                command.Parameters["@corners"].Value = csvPlayer[OrderedCsvColumns.IndexOf("corners")];
                command.Parameters["@creativity"].Value = csvPlayer[OrderedCsvColumns.IndexOf("creativity")];
                command.Parameters["@crossing"].Value = csvPlayer[OrderedCsvColumns.IndexOf("crossing")];
                command.Parameters["@decisions"].Value = csvPlayer[OrderedCsvColumns.IndexOf("decisions")];
                command.Parameters["@determination"].Value = csvPlayer[OrderedCsvColumns.IndexOf("determination")];
                command.Parameters["@dirtiness"].Value = csvPlayer[OrderedCsvColumns.IndexOf("dirtiness")];
                command.Parameters["@dribbling"].Value = csvPlayer[OrderedCsvColumns.IndexOf("dribbling")];
                command.Parameters["@finishing"].Value = csvPlayer[OrderedCsvColumns.IndexOf("finishing")];
                command.Parameters["@flair"].Value = csvPlayer[OrderedCsvColumns.IndexOf("flair")];
                command.Parameters["@handling"].Value = csvPlayer[OrderedCsvColumns.IndexOf("handling")];
                command.Parameters["@heading"].Value = csvPlayer[OrderedCsvColumns.IndexOf("heading")];
                command.Parameters["@important_matches"].Value = csvPlayer[OrderedCsvColumns.IndexOf("important_matches")];
                command.Parameters["@influence"].Value = csvPlayer[OrderedCsvColumns.IndexOf("influence")];
                command.Parameters["@injury_proneness"].Value = MaxAttributeRate - int.Parse(csvPlayer[OrderedCsvColumns.IndexOf("injury_proneness")]);
                command.Parameters["@jumping"].Value = csvPlayer[OrderedCsvColumns.IndexOf("jumping")];
                command.Parameters["@long_shots"].Value = csvPlayer[OrderedCsvColumns.IndexOf("long_shots")];
                command.Parameters["@loyalty"].Value = csvPlayer[OrderedCsvColumns.IndexOf("loyality")];
                command.Parameters["@marking"].Value = csvPlayer[OrderedCsvColumns.IndexOf("marking")];
                command.Parameters["@natural_fitness"].Value = csvPlayer[OrderedCsvColumns.IndexOf("natural_fitness")];
                command.Parameters["@off_the_ball"].Value = csvPlayer[OrderedCsvColumns.IndexOf("off_the_ball")];
                command.Parameters["@one_on_ones"].Value = csvPlayer[OrderedCsvColumns.IndexOf("one_on_ones")];
                command.Parameters["@pace"].Value = csvPlayer[OrderedCsvColumns.IndexOf("pace")];
                command.Parameters["@passing"].Value = csvPlayer[OrderedCsvColumns.IndexOf("passing")];
                command.Parameters["@penalties"].Value = csvPlayer[OrderedCsvColumns.IndexOf("penalties")];
                command.Parameters["@positioning"].Value = csvPlayer[OrderedCsvColumns.IndexOf("positioning")];
                command.Parameters["@pressure"].Value = csvPlayer[OrderedCsvColumns.IndexOf("pressure")];
                command.Parameters["@professionalism"].Value = csvPlayer[OrderedCsvColumns.IndexOf("professionalism")];
                command.Parameters["@reflexes"].Value = csvPlayer[OrderedCsvColumns.IndexOf("reflexes")];
                command.Parameters["@set_pieces"].Value = csvPlayer[OrderedCsvColumns.IndexOf("set_pieces")];
                command.Parameters["@sportsmanship"].Value = csvPlayer[OrderedCsvColumns.IndexOf("sportsmanship")];
                command.Parameters["@stamina"].Value = csvPlayer[OrderedCsvColumns.IndexOf("stamina")];
                command.Parameters["@strength"].Value = csvPlayer[OrderedCsvColumns.IndexOf("strength")];
                command.Parameters["@tackling"].Value = csvPlayer[OrderedCsvColumns.IndexOf("tackling")];
                command.Parameters["@teamwork"].Value = csvPlayer[OrderedCsvColumns.IndexOf("teamwork")];
                command.Parameters["@technique"].Value = csvPlayer[OrderedCsvColumns.IndexOf("technique")];
                command.Parameters["@temperament"].Value = csvPlayer[OrderedCsvColumns.IndexOf("temperament")];
                command.Parameters["@throw_ins"].Value = csvPlayer[OrderedCsvColumns.IndexOf("throw_ins")];
                command.Parameters["@versatility"].Value = csvPlayer[OrderedCsvColumns.IndexOf("versatility")];
                command.Parameters["@work_rate"].Value = csvPlayer[OrderedCsvColumns.IndexOf("work_rate")];

                command.ExecuteNonQuery();
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
