// using System.Collections.Concurrent;
// using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
// using Aspire.Hosting.Testing;
// using Microsoft.EntityFrameworkCore;
// using Respawn;
//
// namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases;
//
// public static class DistributedApplicationFactoryExtensions
// {
//     extension(DistributedApplicationFactory appHost)
//     {
//         public async ValueTask<TDbContext> GetDbContextAsync<TDbContext>()
//             where TDbContext : DbContext, IModuleDbContext
//         {
//             var connectionString = await appHost.GetConnectionString("admitto-db")
//                                    ?? throw new InvalidOperationException(
//                                        "Connection string for PostgreSQL database not found.");
//
//             var options = new DbContextOptionsBuilder<TDbContext>()
//                 .UseNpgsql(
//                     connectionString,
//                     npgsql =>
//                     {
//                         npgsql.MigrationsHistoryTable("ef_migrations_history", TDbContext.SchemaName);
//                     })
//                 .Options;
//
//             return (TDbContext)Activator.CreateInstance(typeof(TDbContext), [options])!;
//         }
//
//         public async ValueTask<TDbContext> ResetDatabaseAsync<TDbContext>(
//             string schemaName,
//             Action<TDbContext>? seed = null,
//             CancellationToken cancellationToken = default)
//             where TDbContext : DbContext, IModuleDbContext
//         {
//             var dbContext = await appHost.GetDbContextAsync<TDbContext>();
//
//             await appHost.RespawnDatabaseAsync(
//                 dbContext,
//                 seed,
//                 cancellationToken);
//
//             return dbContext;
//         }
//     }
//
//     private static ConcurrentDictionary<string, Task<Respawner>> _respawners = new();
//
//     public static async ValueTask RespawnDatabaseAsync<TDbContext>(
//         this DistributedApplicationFactory appHost,
//         TDbContext dbContext,
//         Action<TDbContext>? seed = null,
//         CancellationToken cancellationToken = default)
//         where TDbContext : DbContext
//     {
//         var respawner = await GetRespawnerAsync(dbContext, cancellationToken);
//
//         await respawner.ResetAsync(dbContext.Database.GetDbConnection());
//
//         if (seed is not null)
//         {
//             seed(dbContext);
//             await dbContext.SaveChangesAsync(cancellationToken);
//         }
//     }
//
//     private static async ValueTask<Respawner> GetRespawnerAsync<TDbContext>(
//         TDbContext dbContext,
//         CancellationToken cancellationToken = default)
//         where TDbContext : DbContext
//     {
//         var schemaName = dbContext.Model.GetDefaultSchema()
//                          ?? throw new InvalidOperationException(
//                              "Schema name could not be determined from the DbContext.");
//
//         var getRespawnerTask = _respawners.GetOrAdd(
//             schemaName,
//             async _ =>
//             {
//                 var connection = dbContext.Database.GetDbConnection();
//                 await connection.OpenAsync(cancellationToken);
//
//                 // Ensure EF migrations are applied so the schema exists before creating the respawner.
//                 await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
//
//                 return await Respawner.CreateAsync(
//                     connection,
//                     new RespawnerOptions
//                     {
//                         SchemasToInclude = [schemaName],
//                         TablesToIgnore = ["ef_migrations_history"],
//                         DbAdapter = DbAdapter.Postgres
//                     });
//             });
//
//         return await getRespawnerTask;
//     }
// }