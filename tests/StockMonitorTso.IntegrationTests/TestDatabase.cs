using Npgsql;

namespace StockMonitorTso.IntegrationTests;

/// <summary>
/// Database terpisah per factory di atas Postgres compose (`docker compose up -d postgres`).
/// Admin connection ke db `stockmonitor`; tiap factory dapat `sm_test_{guid}` yang di-drop
/// setelah selesai. Tanpa lifecycle kontainer di dalam testhost.
/// </summary>
public static class TestDatabase
{
    public const string AdminConnectionString =
        "Host=localhost;Port=5432;Database=stockmonitor;Username=stockmonitor;Password=stockmonitor";

    public static async Task<string> CreateDatabaseAsync(CancellationToken ct = default)
    {
        var dbName = $"sm_test_{Guid.NewGuid():N}";
        await using var conn = new NpgsqlConnection(AdminConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
        cmd.CommandTimeout = 60;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Database = dbName,
            Pooling = false,
        };
        return builder.ConnectionString;
    }

    public static async Task DropDatabaseAsync(string connectionString, CancellationToken ct = default)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var dbName = builder.Database;
            builder.Database = "stockmonitor";
            await using var conn = new NpgsqlConnection(builder.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP DATABASE IF EXISTS \"{dbName}\" WITH (FORCE)";
            cmd.CommandTimeout = 60;
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        catch (PostgresException)
        {
            // best effort: database sisa tidak mengganggu run berikutnya (nama unik per factory)
        }
    }
}
