using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Infrastructure.Migrators;

public class MigrationService(IServiceProvider serviceProvider) : IMigrationService
{
    public IEnumerable<string> GetSupportedMigrations()
    {
        return ["database", "openfga", "quartz"];
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
            "openfga" => serviceProvider.GetRequiredService<OpenFgaMigrator>(),
            "quartz" => serviceProvider.GetRequiredService<QuartzMigrator>(),
            _ => throw new NotSupportedException($"Migrator '{migrationName}' is not supported.")
        };
}