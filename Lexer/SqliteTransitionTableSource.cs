using Microsoft.Data.Sqlite;

namespace BananaCompiler.Lexer;

public static class SqliteTransitionTableSource
{
    public static TransitionTable Load(string databaseFile, string tableName = "banana")
    {
        return new TransitionTable(ReadRows(databaseFile, tableName));
    }

    public static IEnumerable<TransitionTableRow> ReadRows(string databaseFile, string tableName = "banana")
    {
        if (!IsSafeIdentifier(tableName))
            throw new ArgumentException("Invalid table name.", nameof(tableName));

        using var connection = new SqliteConnection($"Data Source={databaseFile}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {tableName}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            int state = reader.GetInt32(reader.GetOrdinal("Estado"));
            string? token = null;
            string? message = null;
            var transitions = new Dictionary<string, int>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string column = reader.GetName(i);
                if (column == "Estado")
                    continue;

                if (column == "Token")
                {
                    if (!reader.IsDBNull(i) && !string.IsNullOrWhiteSpace(reader.GetString(i)))
                        token = reader.GetString(i);

                    continue;
                }

                if (column == "Mensaje")
                {
                    if (!reader.IsDBNull(i) && !string.IsNullOrWhiteSpace(reader.GetString(i)))
                        message = reader.GetString(i);

                    continue;
                }

                if (!reader.IsDBNull(i))
                    transitions[column] = reader.GetInt32(i);
            }

            yield return new TransitionTableRow(state, transitions, token, message);
        }
    }

    private static bool IsSafeIdentifier(string value)
    {
        return value.All(character => char.IsLetterOrDigit(character) || character == '_');
    }
}
