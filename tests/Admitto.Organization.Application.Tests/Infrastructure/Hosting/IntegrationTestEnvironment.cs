using Amolenk.Admitto.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Aspire.Hosting.Testing;

namespace Amolenk.Admitto.Organization.Application.Tests.Infrastructure.Hosting;

public sealed record IntegrationTestEnvironment(DatabaseTestContext<OrganizationDbContext> Database)
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

        var organizationDatabase =
            await DatabaseTestContext<OrganizationDbContext>.CreateAsync(
                databaseConnectionString,
                cancellationToken);

        return new IntegrationTestEnvironment(organizationDatabase);
    }
}