using MySql.Data.MySqlClient;

namespace FmFileParse;
internal static class OriginalImporter
{
    internal static void Import(string connString)
    {
        const string FolderPath = @"S:\Share_VM\extract";

        string[] Separators = ["\r\n", "\r", "\n"];

        var done = new List<string> { "01.csv", "02.csv", "03.csv" };

        var notIntCols = new List<string> { "filename", "name", "nation", "club", "position", "scout_rating", "contract_expiration", "contract_type", "date_of_birth", "squad_status", "transfer_status" };
        var columns = new List<string> { "filename", "name", "nation", "club", "position", "ability", "potential_ability", "age", "value", "scout_rating", "acceleration", "adaptability", "aggression", "agility", "ambition", "anticipation", "balance", "bravery", "caps", "club_reputation", "consistency", "contract_expiration", "contract_type", "corners", "creativity", "crossing", "current_reputation", "date_of_birth", "decisions", "determination", "dirtiness", "dribbling", "finishing", "flair", "handling", "heading", "home_reputation", "important_matches", "influence", "injury_proneness", "international_goals", "jumping", "left_foot", "long_shots", "loyality", "manager_job_rel", "marking", "min_fee_rel", "natural_fitness", "non_play_rel", "non_promotion_rel", "off_the_ball", "one_on_ones", "pace", "passing", "penalties", "positioning", "pressure", "professionalism", "reflexes", "relegation_rel", "right_foot", "set_pieces", "sportsmanship", "squad_status", "stamina", "strength", "tackling", "teamwork", "technique", "temperament", "throw_ins", "transfer_status", "versatility", "wage", "work_rate", "world_reputation" };

        using var conn = new MySqlConnection(connString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"insert into raw_data ({string.Join(", ", columns)}) values ({string.Join(", ", columns.Select(x => $"@{x}"))});";
        foreach (var column in columns)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@{column}";
            p.DbType = notIntCols.Contains(column) ? System.Data.DbType.String : System.Data.DbType.Int32;
            p.Direction = System.Data.ParameterDirection.Input;
            cmd.Parameters.Add(p);
        }
        cmd.Prepare();

        foreach (var filePath in Directory.GetFiles(FolderPath, "*.csv", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(filePath);

            if (done.Contains(fileName))
            {
                continue;
            }

            Console.WriteLine($"Process file: {fileName}");

            using var sg = new StreamReader(filePath, System.Text.Encoding.Latin1);
            var fileContent = sg.ReadToEnd();
            var rows = fileContent.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

            var x = 0;
            foreach (var row in rows.Skip(1))
            {
                var cols = row.Split(';');
                if (cols.Length != columns.Count - 1)
                {
                    throw new Exception($"Invalid row line {x} of file {fileName}");
                }
                x++;
            }

            foreach (var row in rows.Skip(1))
            {
                var cols = row.Split(';');
                for (var i = 0; i < columns.Count; i++)
                {
                    if (i == 0)
                    {
                        cmd.Parameters[i].Value = fileName;
                    }
                    else
                    {
                        cmd.Parameters[i].Value = notIntCols.Contains(columns[i]) ? cols[i - 1] : int.Parse(cols[i - 1]);
                    }
                }
                cmd.ExecuteNonQuery();
            }
        }
    }
}
