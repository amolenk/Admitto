using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Application.Tests;

public abstract class DistributedAppTestBase
{
    protected ApplicationContext Context { get; private set; } = null!;

    protected HttpClient Api { get; private set; } = null!;

    protected DefaultTestData TestData { get; private set; } = null!;
    
    private readonly ILogger<DistributedAppTestBase> _logger;
    
    protected DistributedAppTestBase()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
            });
        });
        _logger = loggerFactory.CreateLogger<DistributedAppTestBase>();

        TestData = new DefaultTestData(Guid.Empty, Guid.Empty);
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Set by the test framework.")] 
    public TestContext TestContext { get; set; } = null!;
    
    [TestInitialize]
    public async ValueTask TestInitialize()
    {
        // Create a new instance of the database context.
        Context = DistributedAppRunner.CreateApplicationContext();

        // Reset the database before each non-parallelized test.
        // This is done to ensure that each test starts with a clean state.
        // We skip the database reset if the test method is parallelized because those tests don't care about the
        // database state.
        if (!IsParallelizedTest())
        {
            await ResetDatabaseAsync();
        }
        
        // Create an HttpClient to call the API.
        Api = DistributedAppRunner.CreateApiClient();
    }

    [TestCleanup]
    public async ValueTask TestCleanup()
    {
        await Context.DisposeAsync();
        Api.Dispose();
    }
    
    protected async Task SaveAndClearChangeTrackerAsync()
    {
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }
    
    private bool IsParallelizedTest()
    {
        var testMethod = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == TestContext.TestName);

        if (testMethod == null) return false;
        
        var doNotParallelizeAttribute = testMethod.GetCustomAttribute<DoNotParallelizeAttribute>();
        return doNotParallelizeAttribute == null;
    }

    private async Task ResetDatabaseAsync()
    {
        // Check if the database exists.
        var databaseMigrated = await HasDatabaseBeenMigratedAsync();
        
        // If the database is already migrated, we need to reset it.
        if (databaseMigrated)
        {
            _logger.LogInformation("Deleting existing database...");
            
            // We cannot delete the database without disabling logical replication first.
            await Context.Database.ExecuteSqlRawAsync(
                $"DROP PUBLICATION IF EXISTS {PgOutboxMessageProcessor.PublicationName};");
            await Context.Database.ExecuteSqlRawAsync(
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE pid = (SELECT active_pid FROM pg_replication_slots WHERE slot_name = '{PgOutboxMessageProcessor.SlotName}');");
            await Context.Database.ExecuteSqlRawAsync(
                $"SELECT pg_drop_replication_slot('{PgOutboxMessageProcessor.SlotName}');");
        
            // Drop the database.
            await Context.Database.EnsureDeletedAsync();
        }
        
        // Create the database with migrations.
        _logger.LogInformation("Migrating database...");
        await Context.Database.MigrateAsync();

        _logger.LogInformation("Seeding database...");
        await SeedDatabaseAsync();
    }

    private async Task<bool> HasDatabaseBeenMigratedAsync()
    {
        var connection = Context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            // Check if the __EFMigrationsHistory table exists
            await using var checkTableCommand = connection.CreateCommand();
            checkTableCommand.CommandText = """
                                                SELECT EXISTS (
                                                    SELECT 1
                                                    FROM information_schema.tables 
                                                    WHERE table_schema = 'public'
                                                    AND table_name = '__EFMigrationsHistory'
                                                );
                                            
                                            """;

            var tableExists = (bool)(await checkTableCommand.ExecuteScalarAsync() ?? false);
            if (!tableExists)
            {
                // No migrations have ever been applied
                return false;
            }

            // If the table exists, check if it contains any migrations
            await using var countCommand = connection.CreateCommand();
            countCommand.CommandText = """SELECT COUNT(*) FROM "__EFMigrationsHistory";""";
            var count = (long)(await countCommand.ExecuteScalarAsync() ?? 0);

            return count > 0;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private async Task SeedDatabaseAsync()
    {
        var ticketType = TestDataBuilder.CreateTicketType(name: "Default Ticket Type");
        
        var ticketedEvent = TestDataBuilder.CreateTicketedEvent(name: "Default Event");
        ticketedEvent.AddTicketType(ticketType);
     
        var team = TestDataBuilder.CreateTeam(name: "Default Team");
        team.AddActiveEvent(ticketedEvent);

        Context.Teams.Add(team);
        await SaveAndClearChangeTrackerAsync();

        TestData = new DefaultTestData(team.Id, ticketedEvent.Id);
    }
}

