using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;

public sealed record EndToEndTestEnvironment(
    DatabaseTestContext<OrganizationDbContext> OrganizationDatabase,
    DatabaseTestContext<RegistrationsDbContext> RegistrationsDatabase,
    DatabaseTestContext<EmailDbContext> EmailDatabase,
    MessagingTestContext Messaging,
    EmailTestContext Email,
    HttpClient ApiClient,
    HttpClient BobApiClient,
    HttpClient PublicApiClient,
    DistributedApplication Application)
{
    public static async ValueTask<EndToEndTestEnvironment> CreateAsync(
        EndToEndTestAppHost appHost,
        CancellationToken cancellationToken = default)
    {
        var organizationDatabase =
            await DatabaseTestContext<OrganizationDbContext>.CreateAsync(appHost, cancellationToken);

        var registrationsDatabase =
            await DatabaseTestContext<RegistrationsDbContext>.CreateAsync(appHost, cancellationToken);

        var emailDatabase =
            await DatabaseTestContext<EmailDbContext>.CreateAsync(appHost, cancellationToken);

        var messaging = await MessagingTestContext.CreateAsync(appHost);

        var email = await EmailTestContext.CreateAsync(appHost.Application);

        var factory = appHost.Application.Services.GetRequiredService<IHttpClientFactory>();
        var apiClient = factory.CreateClient("AdmittoApi");
        var bobApiClient = factory.CreateClient("AdmittoApiBob");
        var publicApiClient = factory.CreateClient("AdmittoApiPublic");

        return new EndToEndTestEnvironment(
            organizationDatabase,
            registrationsDatabase,
            emailDatabase,
            messaging,
            email,
            apiClient,
            bobApiClient,
            publicApiClient,
            appHost.Application);
    }

    public HttpClient CreatePublicApiClient(string rawApiKey)
    {
        var client = new HttpClient { BaseAddress = PublicApiClient.BaseAddress };
        client.DefaultRequestHeaders.Add("X-Api-Key", rawApiKey);
        return client;
    }
}
