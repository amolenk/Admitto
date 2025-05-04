using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Amolenk.Admitto.Worker;

public class MigrationHealthCheck : IHealthCheck
{
    private readonly ApplicationContext _dbContext;

    public MigrationHealthCheck(ApplicationContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingMigrations.Any())
            {
                return HealthCheckResult.Unhealthy("Database migrations are pending.");
            }

            return HealthCheckResult.Healthy("Database is up-to-date.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to check database migrations.", ex);
        }
    }
}