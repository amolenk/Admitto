using Amolenk.Admitto.Module.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;

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