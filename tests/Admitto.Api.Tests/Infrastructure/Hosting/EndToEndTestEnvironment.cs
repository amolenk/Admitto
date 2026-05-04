using Amolenk.Admitto.Module.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;

public sealed record EndToEndTestEnvironment(
    DatabaseTestContext<OrganizationDbContext> OrganizationDatabase,
    DatabaseTestContext<RegistrationsDbContext> RegistrationsDatabase,
    DatabaseTestContext<EmailDbContext> EmailDatabase,
    HttpClient MailDevClient,
    Uri MailDevSmtpEndpoint,
    HttpClient ApiClient,
    HttpClient BobApiClient,
    HttpClient PublicApiClient)
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

        var emailDatabase =
            await DatabaseTestContext<EmailDbContext>.CreateAsync(
                databaseConnectionString,
                cancellationToken);

        var factory = appHost.Application.Services.GetRequiredService<IHttpClientFactory>();
        var apiClient = factory.CreateClient("AdmittoApi");
        var bobApiClient = factory.CreateClient("AdmittoApiBob");
        var publicApiClient = factory.CreateClient("AdmittoApiPublic");
        var mailDevClient = factory.CreateClient("MailDev");
        var mailDevSmtpEndpoint = appHost.Application.GetEndpoint("maildev", "smtp");

        return new EndToEndTestEnvironment(organizationDatabase, registrationsDatabase, emailDatabase, mailDevClient, mailDevSmtpEndpoint, apiClient, bobApiClient, publicApiClient);
    }

    public HttpClient CreatePublicApiClient(string rawApiKey)
    {
        var client = new HttpClient { BaseAddress = PublicApiClient.BaseAddress };
        client.DefaultRequestHeaders.Add("X-Api-Key", rawApiKey);
        return client;
    }
}