using Microsoft.Data.SqlClient;

namespace attendence.Data;

public static class DatabaseInitializer
{
    public static void InitializeDatabase(string connectionString, ILogger logger)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogError("❌ No connection string found");
            return;
        }

        try
        {
            // Execute schema script
            var schemaPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Database", "Schema.sql");
            if (!File.Exists(schemaPath))
            {
                schemaPath = "Database/Schema.sql";
            }

            if (File.Exists(schemaPath))
            {
                var schemaScript = File.ReadAllText(schemaPath);
                ExecuteSqlScript(connectionString, schemaScript, logger);
                logger.LogInformation("✅ Database schema initialized");
            }
            else
            {
                logger.LogWarning("⚠️ Schema.sql not found at {Path}", schemaPath);
            }

            // Execute seed script
            var seedPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Database", "DemoSeed.sql");
            if (!File.Exists(seedPath))
            {
                seedPath = "Database/DemoSeed.sql";
            }

            if (File.Exists(seedPath))
            {
                var seedScript = File.ReadAllText(seedPath);
                ExecuteSqlScript(connectionString, seedScript, logger);
                logger.LogInformation("✅ Database seeded with demo data");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Database initialization failed");
        }
    }

    private static void ExecuteSqlScript(string connectionString, string script, ILogger logger)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        var batches = script.Split(new[] { "\nGO", "\r\nGO", "GO" }, StringSplitOptions.None);

        foreach (var batch in batches)
        {
            var trimmedBatch = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmedBatch)) continue;

            using var command = connection.CreateCommand();
            command.CommandText = trimmedBatch;
            command.CommandTimeout = 60;

            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                // Log but continue - some batches may fail if objects already exist
                logger.LogDebug("SQL Batch warning (non-critical): {Message}", ex.Message);
            }
        }
    }
}
