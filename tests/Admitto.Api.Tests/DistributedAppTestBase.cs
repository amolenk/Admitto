using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Application.Tests;

public abstract class DistributedAppTestBase
{
    protected ApplicationContext Context { get; private set; } = null!;

    protected HttpClient Api { get; private set; } = null!;
    
    [TestInitialize]
    public async ValueTask TestInitialize()
    {
        // Create a new instance of the database context.
        Context = DistributedAppTestContext.CreateApplicationContext();
        
        // We cannot delete the database without disabling logical replication first.
        await Context.Database.ExecuteSqlRawAsync(
            $"DROP PUBLICATION IF EXISTS {PgOutboxMessageProcessor.PublicationName};");
        await Context.Database.ExecuteSqlRawAsync(
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE pid = (SELECT active_pid FROM pg_replication_slots WHERE slot_name = '{PgOutboxMessageProcessor.SlotName}');");
        await Context.Database.ExecuteSqlRawAsync(
            $"SELECT pg_drop_replication_slot('{PgOutboxMessageProcessor.SlotName}');");
        
        // Reset the database to initial state.
        await Context.Database.EnsureDeletedAsync();
        await Context.Database.MigrateAsync();
        
        // Create an HttpClient to call the API.
        Api = DistributedAppTestContext.CreateApiClient();
    }

    [TestCleanup]
    public async ValueTask TestCleanup()
    {
        await Context.DisposeAsync();
        Api.Dispose();
    }
}
