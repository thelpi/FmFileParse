﻿using System.Data;
using MySql.Data.MySqlClient;

namespace FmFileParse;

internal static class MySqlHelpers
{
    /// <summary>
    /// Creates and adds a parameter to the command.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="name">Name with or without '@'.</param>
    /// <param name="type"></param>
    /// <param name="value">Use <c>null</c> to not set any value.</param>
    internal static void SetParameter(this MySqlCommand command, string name, DbType type, object? value = null)
    {
        var pCol = command.CreateParameter();
        pCol.ParameterName = name.StartsWith("@") ? name : $"@{name}";
        pCol.DbType = type;
        if (value is not null)
        {
            pCol.Value = value;
        }
        command.Parameters.Add(pCol);
    }

    /// <summary>
    /// Generates the SQL query from the table name and its columns; all parameters will have the same name as columns with '@' prefix.
    /// </summary>
    /// <param name="columns"></param>
    /// <param name="table"></param>
    /// <returns></returns>
    internal static string GetInsertQuery(this IEnumerable<string> columns, string table)
    {
        return $"INSERT INTO {table} ({string.Join(", ", columns)}) " +
            $"VALUES ({string.Join(", ", columns.Select(x => $"@{x}"))})";
    }

    /// <summary>
    /// Executes <see cref="MySqlCommand.ExecuteNonQuery"/> without throwing exception for error number <c>1091</c>.
    /// </summary>
    /// <param name="command"></param>
    internal static void ExecuteNonQuerySecured(this MySqlCommand command)
    {
        try
        {
            command.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (ex.Number == 1091)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }
}
