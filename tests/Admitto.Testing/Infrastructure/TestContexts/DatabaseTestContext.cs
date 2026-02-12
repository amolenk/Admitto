using Amolenk.Admitto.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Shared.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Respawn;

namespace Amolenk.Admitto.Testing.Infrastructure.TestContexts;

public class DatabaseTestContext<TDbContext> : IAsyncDisposable
    where TDbContext : DbContext, IModuleDbContext
{
    private readonly Respawner _respawner;

    public TDbContext Context { get; }

    private DatabaseTestContext(TDbContext context, Respawner respawner)
    {
        Context = context;

        _respawner = respawner;
    }

    public static async ValueTask<DatabaseTestContext<TDbContext>> CreateAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<TDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => { npgsql.MigrationsHistoryTable("ef_migrations_history", TDbContext.SchemaName); })
            // Add the audit interceptor to ensure that audit fields are properly set during tests.
            .AddInterceptors(new AuditInterceptor(new FakeUserContextAccessor()))
            .Options;

        var dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext), options)!;

        await dbContext.Database.MigrateAsync(cancellationToken);

        var respawner = await CreateRespawnerAsync(dbContext, cancellationToken);

        return new DatabaseTestContext<TDbContext>(dbContext, respawner);
    }

    public async Task ResetAsync()
    {
        await _respawner.ResetAsync(Context.Database.GetDbConnection());

        Context.ChangeTracker.Clear();
    }
    
    public async ValueTask SeedAsync(Action<TDbContext> seed, CancellationToken cancellationToken = default)
    {
        seed(Context);
    
        await Context.SaveChangesAsync(cancellationToken);

        // Reset the database context to ensure no stale data is present.
        Context.ChangeTracker.Clear();
    }

    public async ValueTask AssertAsync(Func<TDbContext, ValueTask> operation)
    {
        // Save changes first so we can ensure that the changes can actually be saved to the database before we
        // execute the assertion operation.
        await Context.SaveChangesAsync();
        
        await operation(Context);
    }

    
    public async ValueTask WithContextAsync(Func<TDbContext, ValueTask> operation)
    {
        await operation(Context);
    }

    public async ValueTask DisposeAsync() => await Context.DisposeAsync();

    private static async Task<Respawner> CreateRespawnerAsync(
        TDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var schemaName = dbContext.Model.GetDefaultSchema()
                         ?? throw new InvalidOperationException(
                             "Schema name could not be determined from the DbContext.");

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        return await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                SchemasToInclude = [schemaName],
                TablesToIgnore = ["ef_migrations_history"],
                DbAdapter = DbAdapter.Postgres
            });
    }
}