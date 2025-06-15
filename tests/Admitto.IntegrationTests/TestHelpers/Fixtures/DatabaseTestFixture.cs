using Amolenk.Admitto.Infrastructure.Persistence;
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

    private DatabaseTestFixture(string connectionString, Respawner respawner)
    {
        _connectionString = connectionString;
        _respawner = respawner;
        
        Context = new ApplicationContext(new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(connectionString)
            .Options);
    }
    
    public static async ValueTask<DatabaseTestFixture> CreateAsync(TestingAspireAppHost appHost, 
        CancellationToken cancellationToken)
    {
        var connectionString = await appHost.GetConnectionString("admitto-db");
        if (connectionString is null)
        {
            throw new InvalidOperationException(
                "Connection string for PostgreSQL database not found.");
        }

        var respawner = await CreateRespawnerAsync(appHost, cancellationToken);

        return new DatabaseTestFixture(connectionString, respawner);
    }

    public async Task ResetAsync(Action<ApplicationContext>? seed = null,
        CancellationToken cancellationToken = default)
    {
        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
            await _respawner.ResetAsync(connection);
        }

        if (seed is not null)
        {
            seed(Context);
        
            await Context.SaveChangesAsync(cancellationToken);
            Context.ChangeTracker.Clear();
        }
    }
    
    public async ValueTask DisposeAsync() => await Context.DisposeAsync();
    
    private static async Task<Respawner> CreateRespawnerAsync(TestingAspireAppHost appHost,
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

        return await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude = [ "public" ],
            TablesToIgnore = [ "__EFMigrationsHistory" ],
            DbAdapter = DbAdapter.Postgres
        });
    }
}