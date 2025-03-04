using MySql.Data.MySqlClient;

namespace FmFileParse;
internal class OriginalMerger
{
    internal void Merge()
    {
        const string ConnString = "Server=localhost;Database=cm_save_explorer;Uid=root;Pwd=;";

        const int NumberOfFiles = 12;
        const int MinimalFilePresence = 5;

        const string FilenameCol = "filename";
        const string IdCol = "id";
        const string NationCol = "nation";
        const string Nation2Col = "nation_2";
        const char NatSeparator = '/';

        var columns = new List<string> { "name", "date_of_birth", NationCol, Nation2Col, "caps", "international_goals", "right_foot", "left_foot", "ability", "potential_ability", "home_reputation", "current_reputation", "world_reputation", "club", "value", "contract_expiration", "contract_type", "wage", "transfer_status", "squad_status", "club_reputation", "manager_job_rel", "min_fee_rel", "non_play_rel", "non_promotion_rel", "relegation_rel", "position_gk", "position_sw", "position_d", "position_dm", "position_m", "position_am", "position_f", "position_s", "side_left", "side_right", "side_center", "attributes_mental", "attributes_physical", "attributes_technical", "attributes_all", "acceleration", "adaptability", "aggression", "agility", "ambition", "anticipation", "balance", "bravery", "consistency", "corners", "creativity", "crossing", "decisions", "determination", "dirtiness", "dribbling", "finishing", "flair", "handling", "heading", "important_matches", "influence", "injury_proneness", "jumping", "long_shots", "loyality", "marking", "natural_fitness", "off_the_ball", "one_on_ones", "pace", "passing", "penalties", "positioning", "pressure", "professionalism", "reflexes", "set_pieces", "sportsmanship", "stamina", "strength", "tackling", "teamwork", "technique", "temperament", "throw_ins", "versatility", "work_rate" };
        var stringColumns = new List<string> { "name", NationCol, Nation2Col, "club", "contract_type", "squad_status", "transfer_status" };
        var dateColumns = new List<string> { "contract_expiration", "date_of_birth" };

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
        rCmd.CommandText = "select * from raw_data where name = @name";
        var p = rCmd.CreateParameter();
        p.ParameterName = "@name";
        p.DbType = System.Data.DbType.String;
        rCmd.Parameters.Add(p);
        rCmd.Prepare();

        #endregion Prepare standard read command

        #region Prepare check duplicate command

        using var cdConn = new MySqlConnection(ConnString);
        cdConn.Open();
        using var cdCmd = cdConn.CreateCommand();
        cdCmd.CommandText = "select count(*) from raw_data where name = @name group by filename order by count(*) desc";
        var cdP = cdCmd.CreateParameter();
        cdP.ParameterName = "@name";
        cdP.DbType = System.Data.DbType.String;
        cdCmd.Parameters.Add(cdP);
        cdCmd.Prepare();

        #endregion Prepare check duplicate command

        using var conn = new MySqlConnection(ConnString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "select distinct name from raw_data order by name";
        using var nameReader = cmd.ExecuteReader();
        while (nameReader.Read())
        {
            var playerName = nameReader.GetString("name");

            cdCmd.Parameters["@name"].Value = playerName;
            var count = Convert.ToInt32(cdCmd.ExecuteScalar());
            if (count > 1)
            {
                using var conn3 = new MySqlConnection(ConnString);
                conn3.Open();
                using var cmd3 = conn3.CreateCommand();
                cmd3.CommandText = "select count(*) from raw_data where name = @name group by filename, club order by count(*) desc";
                var pN = cmd3.CreateParameter();
                pN.ParameterName = "@name";
                pN.Value = playerName;
                pN.DbType = System.Data.DbType.String;
                cmd3.Parameters.Add(pN);
                count = Convert.ToInt32(cmd3.ExecuteScalar());
                if (count > 1)
                {
                    using var connp2 = new MySqlConnection(ConnString);
                    connp2.Open();
                    using var cmdp2 = connp2.CreateCommand();
                    cmdp2.CommandText = "select name, club, year(date_of_birth) as yb from raw_data where name = @name group by club, year(date_of_birth)";
                    pN = cmdp2.CreateParameter();
                    pN.ParameterName = "@name";
                    pN.Value = playerName;
                    pN.DbType = System.Data.DbType.String;
                    cmdp2.Parameters.Add(pN);
                    using var rd2 = cmdp2.ExecuteReader();
                    while (rd2.Read())
                    {
                        using var connp3 = new MySqlConnection(ConnString);
                        connp3.Open();
                        using var cmdp3 = connp3.CreateCommand();
                        cmdp3.CommandText = "select * from raw_data where name = @name and IFNULL(club, 'TRUCMUCH') = @club and year(date_of_birth) = @yb";
                        pN = cmdp3.CreateParameter();
                        pN.ParameterName = "@name";
                        pN.Value = rd2["name"];
                        pN.DbType = System.Data.DbType.String;
                        cmdp3.Parameters.Add(pN);
                        var p2 = cmdp3.CreateParameter();
                        p2.ParameterName = "@club";
                        p2.Value = rd2.IsDBNull(rd2.GetOrdinal("club")) ? "TRUCMUCH" : rd2["club"];
                        p2.DbType = System.Data.DbType.String;
                        cmdp3.Parameters.Add(p2);
                        var p3 = cmdp3.CreateParameter();
                        p3.ParameterName = "@yb";
                        p3.Value = rd2["yb"];
                        p3.DbType = System.Data.DbType.Int32;
                        cmdp3.Parameters.Add(p3);
                        using var rd3 = cmdp3.ExecuteReader();
                        CreatePlayer(rd3, playerName, wCmd, dateColumns);
                    }
                }
                else
                {
                    using var connp2 = new MySqlConnection(ConnString);
                    connp2.Open();
                    using var cmdp2 = connp2.CreateCommand();
                    cmdp2.CommandText = "select name, club from raw_data where name = @name group by club";
                    pN = cmdp2.CreateParameter();
                    pN.ParameterName = "@name";
                    pN.Value = playerName;
                    pN.DbType = System.Data.DbType.String;
                    cmdp2.Parameters.Add(pN);
                    using var rd2 = cmdp2.ExecuteReader();
                    while (rd2.Read())
                    {
                        using var connp3 = new MySqlConnection(ConnString);
                        connp3.Open();
                        using var cmdp3 = connp3.CreateCommand();
                        cmdp3.CommandText = "select * from raw_data where name = @name and IFNULL(club, 'TRUCMUCH') = @club";
                        pN = cmdp3.CreateParameter();
                        pN.ParameterName = "@name";
                        pN.Value = rd2["name"];
                        pN.DbType = System.Data.DbType.String;
                        cmdp3.Parameters.Add(pN);
                        var p2 = cmdp3.CreateParameter();
                        p2.ParameterName = "@club";
                        p2.Value = rd2.IsDBNull(rd2.GetOrdinal("club")) ? "TRUCMUCH" : rd2["club"];
                        p2.DbType = System.Data.DbType.String;
                        cmdp3.Parameters.Add(p2);
                        using var rd3 = cmdp3.ExecuteReader();
                        CreatePlayer(rd3, playerName, wCmd, dateColumns);
                    }
                }
            }
            else
            {
                rCmd.Parameters["@name"].Value = playerName;
                using var rd = rCmd.ExecuteReader();
                CreatePlayer(rd, playerName, wCmd, dateColumns);
            }
        }

        static void CreatePlayer(MySqlDataReader dataReader, string playerName, MySqlCommand wCmd, List<string> dateColumns)
        {
            var allFilePlayerData = new List<Dictionary<string, object>>(NumberOfFiles);
            while (dataReader.Read())
            {
                var singleFilePlayerData = new Dictionary<string, object>(dataReader.FieldCount);
                for (var i = 0; i < dataReader.FieldCount; i++)
                {
                    if (dataReader.GetName(i) == FilenameCol || dataReader.GetName(i) == IdCol)
                        continue;

                    if (dataReader.GetName(i) == NationCol)
                    {
                        var nationsArray = dataReader.GetString(i).Split(NatSeparator);
                        singleFilePlayerData.Add(NationCol, nationsArray[0]);
                        singleFilePlayerData.Add(Nation2Col, nationsArray.Length == 1 ? DBNull.Value : nationsArray[1]);
                    }
                    else
                    {
                        singleFilePlayerData.Add(dataReader.GetName(i), dataReader[i]);
                    }
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

            foreach (var c in colsAndVals.Keys)
            {
                wCmd.Parameters[$"@{c}"].Value = colsAndVals[c];
            }
            wCmd.ExecuteScalar();

            Console.WriteLine($"Creation du joueur '{playerName}'");
        }
    }
}
