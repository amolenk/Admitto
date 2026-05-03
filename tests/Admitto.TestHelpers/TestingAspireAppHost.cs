using Amolenk.Admitto.Infrastructure.Auth;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.TestHelpers;

public class TestingAspireAppHost()
    : DistributedApplicationFactory(typeof(Projects.Admitto_AppHost), 
        [
            "DcpPublisher:ResourceNameSuffix=test"
        ])
{
    public DistributedApplication Application { get; private set; } = null!;
    
    protected override void OnBuilding(DistributedApplicationBuilder applicationBuilder)
    {
        RemoveAdminUI(applicationBuilder);
        ReplaceVolumeMounts(applicationBuilder);
        ResetContainerLifetimes(applicationBuilder);
        ConfigureTestServices(applicationBuilder);
        
        base.OnBuilding(applicationBuilder);
    }

    protected override void OnBuilt(DistributedApplication application)
    {
        base.OnBuilt(application);
        Application = application;
    }
    
    private void ConfigureTestServices(DistributedApplicationBuilder applicationBuilder)
    {
        var testConfiguration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        applicationBuilder.Services.AddHttpClient("KeycloakApi")
            .AddHttpMessageHandler(() => 
                CreateAccessTokenHandler(testConfiguration.GetSection("KeycloakApi")));

        applicationBuilder.Services.AddHttpClient("AdmittoApi")
            .AddHttpMessageHandler(() =>
            {
                // For the Admitto API, we need to specify the Keycloak endpoint to get the access token.
                // By default, the AccessTokenHandler will use the base URL of the executing request.
                var keycloakEndpoint = Application.GetEndpoint("keycloak").ToString();
                return CreateAccessTokenHandler(testConfiguration.GetSection("AdmittoApi"), keycloakEndpoint);
            });
    }

    private static AccessTokenHandler CreateAccessTokenHandler(IConfigurationSection configSection, 
        string? tokenEndpointBaseUrl = null)
    {
        var options = new AccessTokenOptions();
        configSection.Bind(options);
        return new AccessTokenHandler(Options.Create(options), tokenEndpointBaseUrl);
    }

    private static void RemoveAdminUI(DistributedApplicationBuilder applicationBuilder)
    {
        var ui = applicationBuilder.Resources.First(r => r.Name == "admin-ui");
        applicationBuilder.Resources.Remove(ui);
    }

    private static void ReplaceVolumeMounts(DistributedApplicationBuilder applicationBuilder)
    {
        // Replace all named volume mounts with anonymous volumes so data does not persist between test runs.
        foreach (var resource in applicationBuilder.Resources)
        {
            var volumes = resource.Annotations
                .OfType<ContainerMountAnnotation>()
                .Where(m => m.Type == ContainerMountType.Volume && !string.IsNullOrEmpty(m.Source))
                .ToList();

            foreach (var volume in volumes)
            {
                resource.Annotations.Remove(volume);
                resource.Annotations.Add(new ContainerMountAnnotation(
                    null, volume.Target, ContainerMountType.Volume, volume.IsReadOnly));
            }
        }
    }

    private static void ResetContainerLifetimes(DistributedApplicationBuilder applicationBuilder)
    {
        // Remove Persistent lifetime annotations so containers use the default Session lifetime,
        // meaning they are stopped and removed after each test run.
        foreach (var resource in applicationBuilder.Resources)
        {
            var persistentLifetimes = resource.Annotations
                .OfType<ContainerLifetimeAnnotation>()
                .Where(a => a.Lifetime == ContainerLifetime.Persistent)
                .ToList();

            foreach (var annotation in persistentLifetimes)
            {
                resource.Annotations.Remove(annotation);
            }
        }
    }
}