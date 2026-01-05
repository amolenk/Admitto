namespace Amolenk.Admitto.Application.Common.Migration;

public interface IMigrationService
{
    IEnumerable<string> GetSupportedMigrations();
    
    Task MigrateAsync(string migrationName, CancellationToken cancellationToken = default);

    Task MigrateAllAsync(CancellationToken cancellationToken = default);
}