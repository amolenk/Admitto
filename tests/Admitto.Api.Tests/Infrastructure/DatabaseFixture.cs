using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Amolenk.Admitto.Application.Tests.Infrastructure;

public class DatabaseFixture : IAsyncDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly string _connectionString;
    
    public ApplicationContext Context { get; }

    private DatabaseFixture(string connectionString)
    {
        _connectionString = connectionString;
        
        Context = new ApplicationContext(new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(connectionString)
            .Options);
    }
    
    public static async ValueTask<DatabaseFixture> CreateAsync(TestingAspireAppHost appHost)
    {
        var connectionString = await appHost.GetConnectionString("postgresdb");
        if (connectionString is null)
        {
            throw new InvalidOperationException(
                "Connection string for PostgreSQL database not found.");
        }

        return new DatabaseFixture(connectionString);
    }

    public async ValueTask ResetAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        await StopLogicalLogReplicationAsync(connection, cancellationToken);
        await DeleteDataAsync(connection, cancellationToken);
        await StartLogicalLogReplicationAsync(connection, cancellationToken);
    }

    public async ValueTask SeedDataAsync(Action<ApplicationContext> seed, CancellationToken cancellationToken = default)
    {
        seed(Context);
        
        await Context.SaveChangesAsync(cancellationToken);
        Context.ChangeTracker.Clear();
    }
    
    public async ValueTask DisposeAsync() => await Context.DisposeAsync();

    private static async ValueTask DeleteDataAsync(NpgsqlConnection connection, CancellationToken cancellationToken = default)
    {
        await ExecuteNonQueryAsync("""

                                               TRUNCATE TABLE 
                                                   attendee_activities, 
                                                   attendee_registrations,
                                                   email_messages,
                                                   outbox,
                                                   teams,
                                                   team_members
                                                   RESTART IDENTITY CASCADE;
                                           
                                   """, connection, cancellationToken);
    }
    
    private static async ValueTask StopLogicalLogReplicationAsync(NpgsqlConnection connection, CancellationToken cancellationToken = default)
    {
        await ExecuteNonQueryAsync(
            $"DROP PUBLICATION IF EXISTS {PgOutboxMessageProcessor.PublicationName};", connection,
            cancellationToken);

        await ExecuteNonQueryAsync(
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE pid = (SELECT active_pid FROM pg_replication_slots WHERE slot_name = '{PgOutboxMessageProcessor.SlotName}');", 
            connection, cancellationToken);

        await ExecuteNonQueryAsync(
            $"SELECT pg_drop_replication_slot('{PgOutboxMessageProcessor.SlotName}');", 
            connection, cancellationToken);
    }
    
    private static async ValueTask StartLogicalLogReplicationAsync(NpgsqlConnection connection, CancellationToken cancellationToken = default)
    {
        await ExecuteNonQueryAsync(
            $"CREATE PUBLICATION {PgOutboxMessageProcessor.PublicationName} FOR TABLE outbox;", 
            connection, cancellationToken);
        
        await ExecuteNonQueryAsync(
            $"SELECT * FROM pg_create_logical_replication_slot('{PgOutboxMessageProcessor.SlotName}', 'pgoutput');", 
            connection, cancellationToken);
    }
    
    private static async ValueTask ExecuteNonQueryAsync(string commandText, NpgsqlConnection connection,
        CancellationToken cancellationToken = default)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        
        await command.ExecuteNonQueryAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
    }
}