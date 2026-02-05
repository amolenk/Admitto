using Amolenk.Admitto.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;

public sealed record EndToEndTestEnvironment(
    DatabaseTestContext<OrganizationDbContext> OrganizationDatabase,
    DatabaseTestContext<RegistrationsDbContext> RegistrationsDatabase,
    HttpClient ApiClient)
{
    public static async ValueTask<EndToEndTestEnvironment> CreateAsync(
        EndToEndTestAppHost appHost,
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

        var registrationsDatabase =
            await DatabaseTestContext<RegistrationsDbContext>.CreateAsync(
                databaseConnectionString,
                cancellationToken);

        var apiClient = appHost.Application.Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient("AdmittoApi");

        return new EndToEndTestEnvironment(organizationDatabase, registrationsDatabase, apiClient);
    }
}