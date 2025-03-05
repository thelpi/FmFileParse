// See https://aka.ms/new-console-template for more information

using FmFileParse;

const string ConnString = "Server=localhost;Database=cm_save_explorer;Uid=root;Pwd=;";

var merger = new PlayersMerger(ConnString, 12, x => Console.WriteLine(x.Item2 ? $"[PASS] {x.Item1}" : $"[KO] {x.Item1}"));

merger.ProceedToMerge(true);

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

*/


Console.WriteLine("Fin de l'importation.");
Console.ReadKey();