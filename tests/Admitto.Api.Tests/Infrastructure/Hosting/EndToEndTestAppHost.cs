using Amolenk.Admitto.Api.Tests.Infrastructure.Api;
using Amolenk.Admitto.Testing.Infrastructure.Hosting;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;

public sealed class EndToEndTestAppHost() : DistributedApplicationFactory(typeof(Projects.Admitto_AppHost))
{
    public DistributedApplication Application { get; private set; } = null!;

    protected override void OnBuilderCreating(
        DistributedApplicationOptions applicationOptions,
        HostApplicationBuilderSettings hostOptions)
    {
        hostOptions.Args = ["DcpPublisher:ResourceNameSuffix=test"];
        hostOptions.EnvironmentName = "EndToEndTesting";
    }

    protected override void OnBuilding(DistributedApplicationBuilder applicationBuilder)
    {
        applicationBuilder.AddTestSuffixToVolumeMounts();

        applicationBuilder.Services.AddHttpClient("AdmittoApi")
            .ConfigureHttpClient(client => { client.BaseAddress = Application.GetEndpoint("api"); })
            .AddHttpMessageHandler(() =>
            {
                // For the Admitto API, we need to specify the Keycloak endpoint to get the access token.
                // By default, the AccessTokenHandler will use the base URL of the executing request.
                var keycloakEndpoint = Application.GetEndpoint("keycloak").ToString();

                var accessTokenOptions = new AccessTokenOptions
                {
                    TokenPath = "/realms/admitto/protocol/openid-connect/token",
                    ClientId = "admitto-test-runner",
                    Username = "alice",
                    Password = "alice"
                };

                return new AccessTokenHandler(accessTokenOptions, keycloakEndpoint);
            });
    }

    protected override void OnBuilt(DistributedApplication application)
    {
        Application = application;
    }
}