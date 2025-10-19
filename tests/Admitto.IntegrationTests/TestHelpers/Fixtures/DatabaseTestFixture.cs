using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Fixtures;

public class DatabaseTestFixture : IAsyncDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly string _connectionString;
    private readonly Respawner _respawner;

    public ApplicationContext Context { get; }

    private DatabaseTestFixture(
        string connectionString,
        Respawner respawner,
        IDataProtectionProvider dataProtectionProvider)
    {
        _connectionString = connectionString;
        _respawner = respawner;

        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(connectionString)
            .Options;

        Context = new ApplicationContext(options, dataProtectionProvider);
    }

    public static async ValueTask<DatabaseTestFixture> CreateAsync(
        TestingAspireAppHost appHost,
        CancellationToken cancellationToken)
    {
        var connectionString = await appHost.GetConnectionString("admitto-db");
        if (connectionString is null)
        {
            throw new InvalidOperationException(
                "Connection string for PostgreSQL database not found.");
        }

        // Ensure EF migrations are applied so the schema exists before creating the respawner.
        var migrationOptions = new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(connectionString)
            .Options;

        var dataProtectionProvider = appHost.Application.Services.GetRequiredService<IDataProtectionProvider>();

        await using (var migrationContext = new ApplicationContext(migrationOptions, dataProtectionProvider))
        {
            await migrationContext.Database.MigrateAsync(cancellationToken);
        }

        var respawner = await CreateRespawnerAsync(appHost, cancellationToken);

        return new DatabaseTestFixture(connectionString, respawner, dataProtectionProvider);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await _respawner.ResetAsync(connection);
        
        Context.ChangeTracker.Clear();
    }

    public async ValueTask DisposeAsync() => await Context.DisposeAsync();

    private static async Task<Respawner> CreateRespawnerAsync(
        TestingAspireAppHost appHost,
        CancellationToken cancellationToken)
    {
        var connectionString = await appHost.GetConnectionString("admitto-db");
        if (connectionString is null)
        {
            throw new InvalidOperationException(
                "Connection string for PostgreSQL database not found.");
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                SchemasToInclude = ["public"],
                TablesToIgnore = ["__EFMigrationsHistory"],
                DbAdapter = DbAdapter.Postgres
            });
    }
}