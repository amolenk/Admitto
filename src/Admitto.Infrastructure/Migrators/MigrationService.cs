using Amolenk.Admitto.Application.Common.Migration;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Infrastructure.Migrators;

public class MigrationService(IServiceProvider serviceProvider) : IMigrationService
{
    public IEnumerable<string> GetSupportedMigrations()
    {
        return ["database", "quartz", "better-auth"];
    }

    public async Task MigrateAsync(string migrationName, CancellationToken cancellationToken)
    {
        var migrator = GetMigrator(migrationName);
        await migrator.RunAsync(cancellationToken);
    }
    
    public async Task MigrateAllAsync(CancellationToken cancellationToken)
    {
        var migrationTasks = GetSupportedMigrations()
            .Select(migrationName => MigrateAsync(migrationName, cancellationToken));
        
        await Task.WhenAll(migrationTasks);
    }
    
    private IMigrator GetMigrator(string migrationName) =>
        migrationName.ToLower() switch
        {
            "database" => serviceProvider.GetRequiredService<DatabaseMigrator>(),
            "quartz" => serviceProvider.GetRequiredService<QuartzMigrator>(),
            "better-auth" => serviceProvider.GetRequiredService<BetterAuthMigrator>(),
            _ => throw new NotSupportedException($"Migrator '{migrationName}' is not supported.")
        };
}