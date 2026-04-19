using Amolenk.Admitto.Module.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Aspire.Hosting.Testing;

namespace Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure.Hosting;

public sealed record IntegrationTestEnvironment(DatabaseTestContext<EmailDbContext> Database)
{
    public static async ValueTask<IntegrationTestEnvironment> CreateAsync(
        DistributedApplicationFactory appHost,
        CancellationToken cancellationToken = default)
    {
        var databaseConnectionString = await appHost.GetConnectionString("admitto-db");
        if (databaseConnectionString is null)
            throw new InvalidOperationException("Connection string for Admitto database not found.");

        var database = await DatabaseTestContext<EmailDbContext>.CreateAsync(
            databaseConnectionString,
            cancellationToken);

        return new IntegrationTestEnvironment(database);
    }
}
