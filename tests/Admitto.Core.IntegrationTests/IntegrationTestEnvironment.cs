using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Aspire.Hosting.Testing;

namespace Amolenk.Admitto.Core.IntegrationTests;

public sealed record IntegrationTestEnvironment(
    DatabaseTestContext<EmailDbContext> EmailDatabase,
    DatabaseTestContext<OrganizationDbContext> OrganizationDatabase,
    DatabaseTestContext<RegistrationsDbContext> RegistrationsDatabase)
{
    public static async ValueTask<IntegrationTestEnvironment> CreateAsync(
        DistributedApplicationFactory appHost,
        CancellationToken cancellationToken = default)
    {
        var databaseConnectionString = await appHost.GetConnectionString("admitto-db");
        if (databaseConnectionString is null)
            throw new InvalidOperationException("Connection string for Admitto database not found.");

        var emailDatabase = await DatabaseTestContext<EmailDbContext>.CreateAsync(
            databaseConnectionString,
            cancellationToken);

        var organizationDatabase = await DatabaseTestContext<OrganizationDbContext>.CreateAsync(
            databaseConnectionString,
            cancellationToken);

        var registrationsDatabase = await DatabaseTestContext<RegistrationsDbContext>.CreateAsync(
            databaseConnectionString,
            cancellationToken);


        return new IntegrationTestEnvironment(emailDatabase, organizationDatabase, registrationsDatabase);
    }
}
