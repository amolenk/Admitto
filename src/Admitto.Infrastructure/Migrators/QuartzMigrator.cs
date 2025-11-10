using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Amolenk.Admitto.Infrastructure.Migrators;

public class QuartzMigrator(IConfiguration configuration, ILogger<QuartzMigrator> logger) : IMigrator
{
    public async ValueTask RunAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("quartz-db")
                               ?? throw new InvalidOperationException("Connection string 'quartz-db' not configured.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var tablesExist = await CheckIfTablesExistAsync(connection);
        if (tablesExist)
        {
            logger.LogInformation("Quartz tables already exist. Skipping migration.");
            return;
        }

        logger.LogInformation("Migrating quartz jobs.");
        var migrationScript = await LoadSqlScriptAsync(cancellationToken);

        await ExecuteSqlScriptAsync(connection, migrationScript, cancellationToken);
    }

    private static async ValueTask<bool> CheckIfTablesExistAsync(NpgsqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public');";
        command.CommandType = System.Data.CommandType.Text;
        command.CommandTimeout = 30;

        var existsObj = await command.ExecuteScalarAsync();
        return existsObj is bool and true;
    }

    private static async ValueTask ExecuteSqlScriptAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = tx;
            command.CommandText = sql;
            command.CommandType = System.Data.CommandType.Text;

            await command.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async ValueTask<string> LoadSqlScriptAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(QuartzMigrator).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("quartz.sql", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException($"Embedded resource 'quartz.sql' not found.");
        }

        await using var stream = assembly.GetManifestResourceStream(resourceName)
                                 ?? throw new InvalidOperationException(
                                     $"Failed to open embedded resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}