using Microsoft.EntityFrameworkCore;

namespace TouristGuide.Data;

public static class SchemaUpgrader
{
    public static void Upgrade(TouristGuideContext context)
    {
        context.Database.EnsureCreated();

        AddColumnIfMissing(context, "Cities", "Latitude", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing(context, "Cities", "Longitude", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing(context, "Attractions", "Latitude", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing(context, "Attractions", "Longitude", "REAL NOT NULL DEFAULT 0");
    }

    private static void AddColumnIfMissing(TouristGuideContext context, string table, string column, string definition)
    {
        if (ColumnExists(context, table, column))
        {
            return;
        }

        // Имена таблиц и столбцов заданы константами в Upgrade(), не приходят от пользователя.
        var sql = table switch
        {
            "Cities" when column is "Latitude" or "Longitude" =>
                $"ALTER TABLE \"Cities\" ADD COLUMN \"{column}\" {definition};",
            "Attractions" when column is "Latitude" or "Longitude" =>
                $"ALTER TABLE \"Attractions\" ADD COLUMN \"{column}\" {definition};",
            _ => throw new InvalidOperationException("Unsupported schema change.")
        };

        context.Database.ExecuteSqlRaw(sql);
    }

    private static bool ColumnExists(TouristGuideContext context, string table, string column)
    {
        var connection = context.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{table}\");";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(1);
                if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }
}
