using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Infrastructure.Migrators;

public class MigrationService(IServiceProvider serviceProvider) : IMigrationService
{
    public IEnumerable<string> GetSupportedMigrations()
    {
        return ["database", "openfga"];
    }

    public async ValueTask MigrateAsync(string migrationName, CancellationToken cancellationToken)
    {
        var migrator = GetMigrator(migrationName);
        await migrator.RunAsync(cancellationToken);
    }
    
    private IMigrator GetMigrator(string migrationName) =>
        migrationName.ToLower() switch
        {
            "database" => serviceProvider.GetRequiredService<DatabaseMigrator>(),
            "openfga" => serviceProvider.GetRequiredService<OpenFgaMigrator>(),
            _ => throw new NotSupportedException($"Migrator '{migrationName}' is not supported.")
        };
}