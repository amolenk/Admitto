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
        var emailDatabase = await DatabaseTestContext<EmailDbContext>.CreateAsync(appHost, cancellationToken);

        var organizationDatabase =
            await DatabaseTestContext<OrganizationDbContext>.CreateAsync(appHost, cancellationToken);

        var registrationsDatabase =
            await DatabaseTestContext<RegistrationsDbContext>.CreateAsync(appHost, cancellationToken);

        return new IntegrationTestEnvironment(emailDatabase, organizationDatabase, registrationsDatabase);
    }
}
