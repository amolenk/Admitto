namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IMigrationService
{
    IEnumerable<string> GetSupportedMigrations();
    
    ValueTask MigrateAsync(string migrationName, CancellationToken cancellationToken);
}