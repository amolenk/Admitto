using Amolenk.Admitto.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Aspire.Hosting.Testing;

namespace Amolenk.Admitto.Registrations.Application.Tests.Infrastructure.Hosting;

public sealed record IntegrationTestEnvironment(DatabaseTestContext<RegistrationsDbContext> Database)
{
    public static async ValueTask<IntegrationTestEnvironment> CreateAsync(
        DistributedApplicationFactory appHost,
        CancellationToken cancellationToken = default)
    {
        var databaseConnectionString = await appHost.GetConnectionString("admitto-db");
        if (databaseConnectionString is null)
        {
            throw new InvalidOperationException("Connection string for Admitto database not found.");
        }

        var registrationsDatabase =
            await DatabaseTestContext<RegistrationsDbContext>.CreateAsync(
                databaseConnectionString,
                cancellationToken);

        return new IntegrationTestEnvironment(registrationsDatabase);
    }
}