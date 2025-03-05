using System.Data;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal class DataImporter(string connectionString)
{
    private readonly Func<MySqlConnection> _getConnection =
        () => new MySqlConnection(connectionString);

    public void ImportCountries(string saveFilePath)
    {
        var data = SaveGameHandler.OpenSaveGameIntoMemory(saveFilePath);

        using var connection = _getConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        // TODO: confederation, is_eu
        command.CommandText = "INSERT INTO countries (id, name, is_eu, confederation_id) " +
            "VALUES (@id, @name, 0, 1)";
        command.SetParameter("@id", DbType.Int32);
        command.SetParameter("@name", DbType.String);
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
        command.SetParameter("@id", DbType.Int32);
        command.SetParameter("@name", DbType.String);
        command.SetParameter("@long_name", DbType.String);
        command.SetParameter("@acronym", DbType.String);
        command.SetParameter("@country_id", DbType.Int32);
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

        command.SetParameter("@id", DbType.Int32);
        command.SetParameter("@name", DbType.String);
        command.SetParameter("@long_name", DbType.String);
        command.SetParameter("@country_id", DbType.Int32);
        command.SetParameter("@reputation", DbType.Int32);
        command.SetParameter("@division_id", DbType.Int32);

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
}
