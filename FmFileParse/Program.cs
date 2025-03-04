// See https://aka.ms/new-console-template for more information

using FmFileParse;

const string ConnString = "Server=localhost;Database=cm_save_explorer;Uid=root;Pwd=;";

var merger = new Merger(ConnString, 12, x => Console.WriteLine(x.Item2 ? $"[PASS] {x.Item1}" : $"[KO] {x.Item1}"));

merger.ProceedToMerge(true);

// merge players
/*
 var columns = new List<string>
{
    "id", "occurences", "first_name", "last_name", "common_name", "date_of_birth", "country_id", "secondary_country_id",
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
};

var stringColumns = new[] { "first_name", "last_name", "common_name", "contract_type", "transfer_status", "squad_status" };
var dateColumns = new[] { "date_of_birth", "contract_expiration" };

const int NumberOfFiles = 12;
const int MinimalFilePresence = 4;

#region Prepare write command

using var wConn = new MySqlConnection(ConnString);
wConn.Open();
using var wCmd = wConn.CreateCommand();
wCmd.CommandText = $"insert into players ({string.Join(", ", columns)}) values ({string.Join(", ", columns.Select(x => $"@{x}"))})";
foreach (var c in columns)
{
    var pCol = wCmd.CreateParameter();
    pCol.ParameterName = $"@{c}";
    pCol.DbType = stringColumns.Contains(c)
        ? System.Data.DbType.String
        : (dateColumns.Contains(c)
            ? System.Data.DbType.Date
            : System.Data.DbType.Int32);
    wCmd.Parameters.Add(pCol);
}
wCmd.Prepare();

#endregion Prepare write command

#region Prepare standard read command

using var rConn = new MySqlConnection(ConnString);
rConn.Open();
using var rCmd = rConn.CreateCommand();
rCmd.CommandText = "select * from unmerged_players " +
    "where (first_name = @first_name OR (@first_name IS NULL AND first_name IS NULL)) " +
    "AND (last_name = @last_name OR (@last_name IS NULL AND last_name IS NULL)) " +
    "AND (common_name = @common_name OR (@common_name IS NULL AND common_name IS NULL))";
var p1 = rCmd.CreateParameter();
p1.ParameterName = "@first_name";
p1.DbType = System.Data.DbType.String;
rCmd.Parameters.Add(p1);
var p2 = rCmd.CreateParameter();
p2.ParameterName = "@last_name";
p2.DbType = System.Data.DbType.String;
rCmd.Parameters.Add(p2);
var p3 = rCmd.CreateParameter();
p3.ParameterName = "@common_name";
p3.DbType = System.Data.DbType.String;
rCmd.Parameters.Add(p3);
rCmd.Prepare();

#endregion Prepare standard read command

#region Prepare check duplicate command

using var cdConn = new MySqlConnection(ConnString);
cdConn.Open();
using var cdCmd = cdConn.CreateCommand();
cdCmd.CommandText = "select count(*) from unmerged_players " +
    "where (first_name = @first_name OR (@first_name IS NULL AND first_name IS NULL)) " +
    "AND (last_name = @last_name OR (@last_name IS NULL AND last_name IS NULL)) " +
    "AND (common_name = @common_name OR (@common_name IS NULL AND common_name IS NULL)) " +
    "group by filename order by count(*) desc";
var cdP1 = cdCmd.CreateParameter();
cdP1.ParameterName = "@first_name";
cdP1.DbType = System.Data.DbType.String;
cdCmd.Parameters.Add(cdP1);
var cdP2 = cdCmd.CreateParameter();
cdP2.ParameterName = "@last_name";
cdP2.DbType = System.Data.DbType.String;
cdCmd.Parameters.Add(cdP2);
var cdP3 = cdCmd.CreateParameter();
cdP3.ParameterName = "@common_name";
cdP3.DbType = System.Data.DbType.String;
cdCmd.Parameters.Add(cdP3);
cdCmd.Prepare();

#endregion Prepare check duplicate command

using var conn = new MySqlConnection(ConnString);
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "select distinct common_name, last_name, first_name from unmerged_players order by common_name, last_name, first_name";
using var nameReader = cmd.ExecuteReader();
while (nameReader.Read())
{
    var firstName = nameReader["first_name"];
    var lastName = nameReader["last_name"];
    var commonName = nameReader["common_name"];

    cdCmd.Parameters["@first_name"].Value = firstName;
    cdCmd.Parameters["@last_name"].Value = lastName;
    cdCmd.Parameters["@common_name"].Value = commonName;
    var count = Convert.ToInt32(cdCmd.ExecuteScalar());
    if (count > 1)
    {
        using var conn3 = new MySqlConnection(ConnString);
        conn3.Open();
        using var cmd3 = conn3.CreateCommand();
        cmd3.CommandText = "select count(*) from unmerged_players " +
            "where (first_name = @first_name OR (@first_name IS NULL AND first_name IS NULL)) " +
            "AND (last_name = @last_name OR (@last_name IS NULL AND last_name IS NULL)) " +
            "AND (common_name = @common_name OR (@common_name IS NULL AND common_name IS NULL)) " +
            "group by filename, club_id order by count(*) desc";
        var pN1 = cmd3.CreateParameter();
        pN1.ParameterName = "@first_name";
        pN1.Value = firstName;
        pN1.DbType = System.Data.DbType.String;
        cmd3.Parameters.Add(pN1);
        var pN2 = cmd3.CreateParameter();
        pN2.ParameterName = "@last_name";
        pN2.Value = lastName;
        pN2.DbType = System.Data.DbType.String;
        cmd3.Parameters.Add(pN2);
        var pN3 = cmd3.CreateParameter();
        pN3.ParameterName = "@common_name";
        pN3.Value = commonName;
        pN3.DbType = System.Data.DbType.String;
        cmd3.Parameters.Add(pN3);
        count = Convert.ToInt32(cmd3.ExecuteScalar());
        if (count > 1)
        {
            using var connp2 = new MySqlConnection(ConnString);
            connp2.Open();
            using var cmdp2 = connp2.CreateCommand();
            cmdp2.CommandText = "select first_name, last_name, common_name, club_id, date_of_birth " +
                "from unmerged_players " +
                "where (first_name = @first_name OR (@first_name IS NULL AND first_name IS NULL)) " +
                "AND (last_name = @last_name OR (@last_name IS NULL AND last_name IS NULL)) " +
                "AND (common_name = @common_name OR (@common_name IS NULL AND common_name IS NULL)) " +
                "group by club_id, date_of_birth";
            pN1 = cmdp2.CreateParameter();
            pN1.ParameterName = "@first_name";
            pN1.Value = firstName;
            pN1.DbType = System.Data.DbType.String;
            cmdp2.Parameters.Add(pN1);
            pN2 = cmdp2.CreateParameter();
            pN2.ParameterName = "@last_name";
            pN2.Value = lastName;
            pN2.DbType = System.Data.DbType.String;
            cmdp2.Parameters.Add(pN2);
            pN3 = cmdp2.CreateParameter();
            pN3.ParameterName = "@common_name";
            pN3.Value = commonName;
            pN3.DbType = System.Data.DbType.String;
            cmdp2.Parameters.Add(pN3);
            using var rd2 = cmdp2.ExecuteReader();
            while (rd2.Read())
            {
                using var connp3 = new MySqlConnection(ConnString);
                connp3.Open();
                using var cmdp3 = connp3.CreateCommand();
                cmdp3.CommandText = "select * from unmerged_players " +
                    "where (first_name = @first_name OR (@first_name IS NULL AND first_name IS NULL)) " +
                    "AND (last_name = @last_name OR (@last_name IS NULL AND last_name IS NULL)) " +
                    "AND (common_name = @common_name OR (@common_name IS NULL AND common_name IS NULL)) " +
                    "and (club_id = @club_id OR (@club_id IS NULL and club_id IS NULL)) " +
                    "and date_of_birth = @date_of_birth";
                pN1 = cmdp3.CreateParameter();
                pN1.ParameterName = "@first_name";
                pN1.Value = firstName;
                pN1.DbType = System.Data.DbType.String;
                cmdp3.Parameters.Add(pN1);
                pN2 = cmdp3.CreateParameter();
                pN2.ParameterName = "@last_name";
                pN2.Value = lastName;
                pN2.DbType = System.Data.DbType.String;
                cmdp3.Parameters.Add(pN2);
                pN3 = cmdp3.CreateParameter();
                pN3.ParameterName = "@common_name";
                pN3.Value = commonName;
                pN3.DbType = System.Data.DbType.String;
                cmdp3.Parameters.Add(pN3);
                var pN4 = cmdp3.CreateParameter();
                pN4.ParameterName = "@club_id";
                pN4.Value = rd2["club_id"];
                pN4.DbType = DbType.Int32;
                cmdp3.Parameters.Add(pN4);
                var pN5 = cmdp3.CreateParameter();
                pN5.ParameterName = "@date_of_birth";
                pN5.Value = rd2["date_of_birth"];
                pN5.DbType = DbType.Date;
                cmdp3.Parameters.Add(pN5);
                using var rd3 = cmdp3.ExecuteReader();
                CreatePlayer(rd3, firstName, lastName, commonName, wCmd, dateColumns);
            }
        }
        else
        {
            using var connp2 = new MySqlConnection(ConnString);
            connp2.Open();
            using var cmdp2 = connp2.CreateCommand();
            cmdp2.CommandText = "select first_name, last_name, common_name, club_id " +
                "from unmerged_players " +
                "where (first_name = @first_name OR (@first_name IS NULL AND first_name IS NULL)) " +
                "AND (last_name = @last_name OR (@last_name IS NULL AND last_name IS NULL)) " +
                "AND (common_name = @common_name OR (@common_name IS NULL AND common_name IS NULL)) " +
                "group by club_id";
            var pn1 = cmdp2.CreateParameter();
            pn1.ParameterName = "@first_name";
            pn1.Value = firstName;
            pn1.DbType = System.Data.DbType.String;
            cmdp2.Parameters.Add(pn1);
            var pn2 = cmdp2.CreateParameter();
            pn2.ParameterName = "@last_name";
            pn2.Value = lastName;
            pn2.DbType = System.Data.DbType.String;
            cmdp2.Parameters.Add(pn2);
            var pn3 = cmdp2.CreateParameter();
            pn3.ParameterName = "@common_name";
            pn3.Value = commonName;
            pn3.DbType = System.Data.DbType.String;
            cmdp2.Parameters.Add(pn3);
            using var rd2 = cmdp2.ExecuteReader();
            while (rd2.Read())
            {
                using var connp3 = new MySqlConnection(ConnString);
                connp3.Open();
                using var cmdp3 = connp3.CreateCommand();
                cmdp3.CommandText = "select * from unmerged_players " +
                    "where (first_name = @first_name OR (@first_name IS NULL AND first_name IS NULL)) " +
                    "AND (last_name = @last_name OR (@last_name IS NULL AND last_name IS NULL)) " +
                    "AND (common_name = @common_name OR (@common_name IS NULL AND common_name IS NULL)) " +
                    "and (club_id = @club_id OR (@club_id IS NULL and club_id IS NULL))";
                var pd1 = cmdp3.CreateParameter();
                pd1.ParameterName = "@first_name";
                pd1.Value = rd2["first_name"];
                pd1.DbType = System.Data.DbType.String;
                cmdp3.Parameters.Add(pd1);
                var pd2 = cmdp3.CreateParameter();
                pd2.ParameterName = "@last_name";
                pd2.Value = rd2["last_name"];
                pd2.DbType = System.Data.DbType.String;
                cmdp3.Parameters.Add(pd2);
                var pd3 = cmdp3.CreateParameter();
                pd3.ParameterName = "@common_name";
                pd3.Value = rd2["common_name"];
                pd3.DbType = System.Data.DbType.String;
                cmdp3.Parameters.Add(pd3);
                var pd4 = cmdp3.CreateParameter();
                pd4.ParameterName = "@club_id";
                pd4.Value = rd2["club_id"];
                pd4.DbType = System.Data.DbType.String;
                cmdp3.Parameters.Add(pd4);
                using var rd3 = cmdp3.ExecuteReader();
                CreatePlayer(rd3, firstName, lastName, commonName, wCmd, dateColumns);
            }
        }
    }
    else
    {
        rCmd.Parameters["@first_name"].Value = firstName;
        rCmd.Parameters["@last_name"].Value = lastName;
        rCmd.Parameters["@common_name"].Value = commonName;
        using var rd = rCmd.ExecuteReader();
        CreatePlayer(rd, firstName, lastName, commonName, wCmd, dateColumns);
    }
}

static void CreatePlayer(MySqlDataReader dataReader, object firstName, object lastName, object commonName, MySqlCommand wCmd, string[] dateColumns)
{
    var allFilePlayerData = new List<Dictionary<string, object>>(NumberOfFiles);
    while (dataReader.Read())
    {
        var singleFilePlayerData = new Dictionary<string, object>(dataReader.FieldCount);
        for (var i = 0; i < dataReader.FieldCount; i++)
        {
            if (dataReader.GetName(i) == "filename" || dataReader.GetName(i) == "id")
                continue;

            singleFilePlayerData.Add(dataReader.GetName(i), dataReader[i]);
        }
        allFilePlayerData.Add(singleFilePlayerData);
    }

    if (allFilePlayerData.Count < MinimalFilePresence)
    {
        return;
    }

    var colsAndVals = new Dictionary<string, object>(90);
    foreach (var col in allFilePlayerData[0].Keys)
    {
        var allValues = allFilePlayerData.Select(_ => _[col]).ToList();

        var maxOccurenceValue = allValues.GroupBy(x => x).OrderByDescending(x => x.Count()).First();

        if (maxOccurenceValue.Count() > allFilePlayerData.Count / 2)
        {
            colsAndVals.Add(col, maxOccurenceValue.Key);
        }
        else
        {
            var neverNullValues = allValues.All(x => x != null && x != DBNull.Value);
            if (neverNullValues && int.TryParse(allValues[0].ToString(), out _))
            {
                colsAndVals.Add(col, Convert.ToInt32(Math.Round(allValues.Select(Convert.ToInt32).Average())));
            }
            else if (neverNullValues && dateColumns.Contains(col))
            {
                colsAndVals.Add(col, allValues.Select(Convert.ToDateTime).GetAverageDate());
            }
            else
            {
                colsAndVals.Add(col, maxOccurenceValue.Key);
            }
        }
    }

    colsAndVals.Add("occurences", allFilePlayerData.Count);

    foreach (var c in colsAndVals.Keys)
    {
        wCmd.Parameters[$"@{c}"].Value = colsAndVals[c];
    }
    wCmd.ExecuteScalar();

    Console.WriteLine($"Creation du joueur '{(commonName == DBNull.Value ? string.Concat((lastName == DBNull.Value ? "" : lastName), ", ", (firstName == DBNull.Value ? "" : firstName)) : commonName)}'");
}
 */

// players
/*
var columns = new List<string>
{
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
};

var orderedCsvCols = new List<string>
{
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
};

var sCol = new[] { "filename", "first_name", "last_name", "common_name", "contract_type", "transfer_status", "squad_status" };
var dCol = new[] { "date_of_birth", "contract_expiration" };

using var conn = new MySqlConnection(ConnString);
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = $"INSERT INTO unmerged_players ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(x => $"@{x}"))})";

foreach (var c in columns)
{
    var p = cmd.CreateParameter();
    p.ParameterName = $"@{c}";
    p.DbType = sCol.Contains(c)
        ? System.Data.DbType.String
        : (dCol.Contains(c)
            ? System.Data.DbType.Date
            : System.Data.DbType.Int32);
    cmd.Parameters.Add(p);
}

cmd.Prepare();

var notFound = new List<(string, string, Player)>();

for (var i = 1; i <= 12; i++)
{
    var iF = i.ToString().PadLeft(2, '0');
    var f = $"{iF}.sav";

    var data = SaveGameHandler.OpenSaveGameIntoMemory($"S:\\Share_VM\\saves\\test\\{f}");

    using var sg = new StreamReader($"S:\\Share_VM\\extract\\{iF}.csv", System.Text.Encoding.Latin1);
    var fileContent = sg.ReadToEnd();
    var rows = fileContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

    var dict = new Dictionary<string, string[]>(10000);
    foreach (var row in rows.Skip(1))
    {
        var cols = row.Split(';');
        dict.Add($"{cols[0].Trim()};{cols[orderedCsvCols.IndexOf("world_reputation")]};{cols[orderedCsvCols.IndexOf("home_reputation")]};{cols[orderedCsvCols.IndexOf("current_reputation")]};{cols[4]};{cols[5]}", cols);
    }

    foreach (var p in data.Players)
    {
        var fn = data.FirstNames.ContainsKey(p._staff.FirstNameId) && !string.IsNullOrWhiteSpace(data.FirstNames[p._staff.FirstNameId]) ? (object)data.FirstNames[p._staff.FirstNameId].Trim() : DBNull.Value;
        var ln = data.Surnames.ContainsKey(p._staff.SecondNameId) && !string.IsNullOrWhiteSpace(data.Surnames[p._staff.SecondNameId]) ? (object)data.Surnames[p._staff.SecondNameId].Trim() : DBNull.Value;
        var cn = data.CommonNames.ContainsKey(p._staff.CommonNameId) && !string.IsNullOrWhiteSpace(data.CommonNames[p._staff.CommonNameId]) ? (object)data.CommonNames[p._staff.CommonNameId].Trim() : DBNull.Value;

        if (ln.ToString() == "Martínez\r\nMartínez")
        {
            ln = "Martínez";
        }

        var exp = p._contract != null && p._contract.ContractEndDate.HasValue ? (object)p._contract.ContractEndDate.Value : DBNull.Value;

        var nn = (cn == DBNull.Value ? ($"{ln}, {fn}") : cn.ToString()).Trim();
        var k = string.Concat(nn, ";", p._player.WorldReputation, ";", p._player.Reputation, ";", p._player.DomesticReputation, ";", p._player.CurrentAbility, ";", p._player.PotentialAbility);
        if (!dict.ContainsKey(k))
        {
            notFound.Add((f, nn, p));
            continue;
        }
        var pk = dict[k];

        cmd.Parameters["@id"].Value = p._staff.StaffId;
        cmd.Parameters["@filename"].Value = f;
        cmd.Parameters["@first_name"].Value = fn;
        cmd.Parameters["@last_name"].Value = ln;
        cmd.Parameters["@common_name"].Value = cn;
        cmd.Parameters["@date_of_birth"].Value = p._staff.DOB;
        cmd.Parameters["@country_id"].Value = p._staff.NationId;
        cmd.Parameters["@secondary_country_id"].Value = p._staff.SecondaryNationId >= 0 ? p._staff.SecondaryNationId : DBNull.Value;
        cmd.Parameters["@caps"].Value = p._staff.InternationalCaps;
        cmd.Parameters["@international_goals"].Value = pk[orderedCsvCols.IndexOf("international_goals")];
        cmd.Parameters["@right_foot"].Value = p._player.RightFoot;
        cmd.Parameters["@left_foot"].Value = p._player.LeftFoot;
        cmd.Parameters["@ability"].Value = p._player.CurrentAbility;
        cmd.Parameters["@potential_ability"].Value = p._player.PotentialAbility;
        cmd.Parameters["@home_reputation"].Value = p._player.DomesticReputation;
        cmd.Parameters["@current_reputation"].Value = p._player.Reputation;
        cmd.Parameters["@world_reputation"].Value = p._player.WorldReputation;
        cmd.Parameters["@club_id"].Value = p._staff.ClubId >= 0 ? p._staff.ClubId : System.DBNull.Value;
        cmd.Parameters["@value"].Value = p._staff.Value;
        cmd.Parameters["@contract_expiration"].Value = exp;
        cmd.Parameters["@contract_type"].Value = string.IsNullOrWhiteSpace(pk[orderedCsvCols.IndexOf("contract_type")]) ? DBNull.Value : pk[orderedCsvCols.IndexOf("contract_type")]; // ???
        cmd.Parameters["@wage"].Value = p._staff.Wage;
        cmd.Parameters["@transfer_status"].Value = p._contract != null && Enum.IsDefined((TransferStatus)p._contract.TransferStatus) ? ((TransferStatus)p._contract.TransferStatus).ToString() : DBNull.Value;
        cmd.Parameters["@squad_status"].Value = p._contract != null && Enum.IsDefined((SquadStatus)p._contract.SquadStatus) ? ((SquadStatus)p._contract.SquadStatus).ToString() : DBNull.Value;
        cmd.Parameters["@manager_job_rel"].Value = p._contract != null && p._contract.ManagerReleaseClause ? p._contract.ReleaseClauseValue : 0;
        cmd.Parameters["@min_fee_rel"].Value = p._contract != null && p._contract.MinimumFeeReleaseClause ? p._contract.ReleaseClauseValue : 0;
        cmd.Parameters["@non_play_rel"].Value = p._contract != null && p._contract.NonPlayingReleaseClause ? p._contract.ReleaseClauseValue : 0;
        cmd.Parameters["@non_promotion_rel"].Value = p._contract != null && p._contract.NonPromotionReleaseClause ? p._contract.ReleaseClauseValue : 0;
        cmd.Parameters["@relegation_rel"].Value = p._contract != null && p._contract.RelegationReleaseClause ? p._contract.ReleaseClauseValue : 0;
        cmd.Parameters["@pos_goalkeeper"].Value = p._player.GK;
        cmd.Parameters["@pos_sweeper"].Value = p._player.SW;
        cmd.Parameters["@pos_defender"].Value = p._player.DF;
        cmd.Parameters["@pos_defensive_midfielder"].Value = p._player.DM;
        cmd.Parameters["@pos_midfielder"].Value = p._player.MF;
        cmd.Parameters["@pos_attacking_midfielder"].Value = p._player.AM;
        cmd.Parameters["@pos_forward"].Value = p._player.ST;
        cmd.Parameters["@pos_wingback"].Value = p._player.WingBack;
        cmd.Parameters["@pos_free_role"].Value = p._player.FreeRole;
        cmd.Parameters["@side_left"].Value = p._player.Left;
        cmd.Parameters["@side_right"].Value = p._player.Right;
        cmd.Parameters["@side_center"].Value = p._player.Centre;
        cmd.Parameters["@acceleration"].Value = pk[orderedCsvCols.IndexOf("acceleration")];
        cmd.Parameters["@adaptability"].Value = pk[orderedCsvCols.IndexOf("adaptability")];
        cmd.Parameters["@aggression"].Value = pk[orderedCsvCols.IndexOf("aggression")];
        cmd.Parameters["@agility"].Value = pk[orderedCsvCols.IndexOf("agility")];
        cmd.Parameters["@ambition"].Value = pk[orderedCsvCols.IndexOf("ambition")];
        cmd.Parameters["@anticipation"].Value = pk[orderedCsvCols.IndexOf("anticipation")];
        cmd.Parameters["@balance"].Value = pk[orderedCsvCols.IndexOf("balance")];
        cmd.Parameters["@bravery"].Value = pk[orderedCsvCols.IndexOf("bravery")];
        cmd.Parameters["@consistency"].Value = pk[orderedCsvCols.IndexOf("consistency")];
        cmd.Parameters["@corners"].Value = pk[orderedCsvCols.IndexOf("corners")];
        cmd.Parameters["@creativity"].Value = pk[orderedCsvCols.IndexOf("creativity")];
        cmd.Parameters["@crossing"].Value = pk[orderedCsvCols.IndexOf("crossing")];
        cmd.Parameters["@decisions"].Value = pk[orderedCsvCols.IndexOf("decisions")];
        cmd.Parameters["@determination"].Value = pk[orderedCsvCols.IndexOf("determination")];
        cmd.Parameters["@dirtiness"].Value = pk[orderedCsvCols.IndexOf("dirtiness")];
        cmd.Parameters["@dribbling"].Value = pk[orderedCsvCols.IndexOf("dribbling")];
        cmd.Parameters["@finishing"].Value = pk[orderedCsvCols.IndexOf("finishing")];
        cmd.Parameters["@flair"].Value = pk[orderedCsvCols.IndexOf("flair")];
        cmd.Parameters["@handling"].Value = pk[orderedCsvCols.IndexOf("handling")];
        cmd.Parameters["@heading"].Value = pk[orderedCsvCols.IndexOf("heading")];
        cmd.Parameters["@important_matches"].Value = pk[orderedCsvCols.IndexOf("important_matches")];
        cmd.Parameters["@influence"].Value = pk[orderedCsvCols.IndexOf("influence")];
        cmd.Parameters["@injury_proneness"].Value = 20 - int.Parse(pk[orderedCsvCols.IndexOf("injury_proneness")]);
        cmd.Parameters["@jumping"].Value = pk[orderedCsvCols.IndexOf("jumping")];
        cmd.Parameters["@long_shots"].Value = pk[orderedCsvCols.IndexOf("long_shots")];
        cmd.Parameters["@loyalty"].Value = pk[orderedCsvCols.IndexOf("loyality")];
        cmd.Parameters["@marking"].Value = pk[orderedCsvCols.IndexOf("marking")];
        cmd.Parameters["@natural_fitness"].Value = pk[orderedCsvCols.IndexOf("natural_fitness")];
        cmd.Parameters["@off_the_ball"].Value = pk[orderedCsvCols.IndexOf("off_the_ball")];
        cmd.Parameters["@one_on_ones"].Value = pk[orderedCsvCols.IndexOf("one_on_ones")];
        cmd.Parameters["@pace"].Value = pk[orderedCsvCols.IndexOf("pace")];
        cmd.Parameters["@passing"].Value = pk[orderedCsvCols.IndexOf("passing")];
        cmd.Parameters["@penalties"].Value = pk[orderedCsvCols.IndexOf("penalties")];
        cmd.Parameters["@positioning"].Value = pk[orderedCsvCols.IndexOf("positioning")];
        cmd.Parameters["@pressure"].Value = pk[orderedCsvCols.IndexOf("pressure")];
        cmd.Parameters["@professionalism"].Value = pk[orderedCsvCols.IndexOf("professionalism")];
        cmd.Parameters["@reflexes"].Value = pk[orderedCsvCols.IndexOf("reflexes")];
        cmd.Parameters["@set_pieces"].Value = pk[orderedCsvCols.IndexOf("set_pieces")];
        cmd.Parameters["@sportsmanship"].Value = pk[orderedCsvCols.IndexOf("sportsmanship")];
        cmd.Parameters["@stamina"].Value = pk[orderedCsvCols.IndexOf("stamina")];
        cmd.Parameters["@strength"].Value = pk[orderedCsvCols.IndexOf("strength")];
        cmd.Parameters["@tackling"].Value = pk[orderedCsvCols.IndexOf("tackling")];
        cmd.Parameters["@teamwork"].Value = pk[orderedCsvCols.IndexOf("teamwork")];
        cmd.Parameters["@technique"].Value = pk[orderedCsvCols.IndexOf("technique")];
        cmd.Parameters["@temperament"].Value = pk[orderedCsvCols.IndexOf("temperament")];
        cmd.Parameters["@throw_ins"].Value = pk[orderedCsvCols.IndexOf("throw_ins")];
        cmd.Parameters["@versatility"].Value = pk[orderedCsvCols.IndexOf("versatility")];
        cmd.Parameters["@work_rate"].Value = pk[orderedCsvCols.IndexOf("work_rate")];
        cmd.ExecuteNonQuery();
    }
}
*/

// clubs
/*
using var conn = new MySqlConnection(ConnString);
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "INSERT INTO clubs (id, name, long_name, country_id, reputation) VALUES (@id, @name, @long_name, @country_id, @reputation)";

var p = cmd.CreateParameter();
p.DbType = System.Data.DbType.Int32;
p.ParameterName = "@id";
cmd.Parameters.Add(p);

p = cmd.CreateParameter();
p.DbType = System.Data.DbType.String;
p.ParameterName = "@name";
cmd.Parameters.Add(p);

p = cmd.CreateParameter();
p.DbType = System.Data.DbType.String;
p.ParameterName = "@long_name";
cmd.Parameters.Add(p);

p = cmd.CreateParameter();
p.DbType = System.Data.DbType.Int32;
p.ParameterName = "@country_id";
cmd.Parameters.Add(p);

p = cmd.CreateParameter();
p.DbType = System.Data.DbType.Int32;
p.ParameterName = "@reputation";
cmd.Parameters.Add(p);

cmd.Prepare();

foreach (var key in data.Clubs.Keys)
{
    cmd.Parameters["@id"].Value = data.Clubs[key].ClubId;
    cmd.Parameters["@name"].Value = data.Clubs[key].Name;
    cmd.Parameters["@long_name"].Value = data.Clubs[key].LongName;
    cmd.Parameters["@country_id"].Value = data.Clubs[key].NationId < 0 ? System.DBNull.Value : data.Clubs[key].NationId;
    cmd.Parameters["@reputation"].Value = data.Clubs[key].Reputation;
    cmd.ExecuteNonQuery();
}
*/

// competitions
/*using var conn = new MySqlConnection(ConnString);
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "INSERT INTO competitions (id, name, long_name, acronym, country_id) VALUES (@id, @name, @long_name, @acronym, @country_id)";

var p = cmd.CreateParameter();
p.DbType = System.Data.DbType.Int32;
p.ParameterName = "@id";
cmd.Parameters.Add(p);

p = cmd.CreateParameter();
p.DbType = System.Data.DbType.String;
p.ParameterName = "@name";
cmd.Parameters.Add(p);

p = cmd.CreateParameter();
p.DbType = System.Data.DbType.String;
p.ParameterName = "@long_name";
cmd.Parameters.Add(p);

p = cmd.CreateParameter();
p.DbType = System.Data.DbType.String;
p.ParameterName = "@acronym";
cmd.Parameters.Add(p);

p = cmd.CreateParameter();
p.DbType = System.Data.DbType.Int32;
p.ParameterName = "@country_id";
cmd.Parameters.Add(p);

cmd.Prepare();

foreach (var key in data.ClubComps.Keys)
{
    cmd.Parameters["@id"].Value = data.ClubComps[key].Id;
    cmd.Parameters["@name"].Value = data.ClubComps[key].Name;
    cmd.Parameters["@long_name"].Value = data.ClubComps[key].LongName;
    cmd.Parameters["@acronym"].Value = data.ClubComps[key].Abbreviation;
    cmd.Parameters["@country_id"].Value = data.ClubComps[key].NationId >= 0 && data.ClubComps[key].NationId <= 212 ? data.ClubComps[key].NationId : System.DBNull.Value;
    cmd.ExecuteNonQuery();
}*/

// country
/*using var conn = new MySqlConnection(ConnString);
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "INSERT INTO countries (id, name, is_eu, confederation_id) VALUES (@id, @name, 0, 1)";

var p = cmd.CreateParameter();
p.DbType = System.Data.DbType.Int32;
p.ParameterName = "@id";
cmd.Parameters.Add(p);

p = cmd.CreateParameter();
p.DbType = System.Data.DbType.String;
p.ParameterName = "@name";
cmd.Parameters.Add(p);

cmd.Prepare();

foreach (var key in data.Nations.Keys)
{
    cmd.Parameters["@id"].Value = data.Nations[key].Id;
    cmd.Parameters["@name"].Value = data.Nations[key].Name;
    cmd.ExecuteNonQuery();
}*/

Console.WriteLine("Fin de l'importation.");
Console.ReadKey();