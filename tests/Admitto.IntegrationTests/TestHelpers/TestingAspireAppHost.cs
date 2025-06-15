using Amolenk.Admitto.Infrastructure.Auth;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

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
        RemoveWorker(applicationBuilder);
        ReplaceVolumeMounts(applicationBuilder);
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

    private static void RemoveWorker(DistributedApplicationBuilder applicationBuilder)
    {
        var worker = applicationBuilder.Resources.First(r => r.Name == "worker");
        applicationBuilder.Resources.Remove(worker);
    }

    private static void ReplaceVolumeMounts(DistributedApplicationBuilder applicationBuilder)
    {
        // Replace all volume mounts with the "-test" suffix.
        foreach (var resource in applicationBuilder.Resources)
        {
            var volumes = resource.Annotations
                .OfType<ContainerMountAnnotation>()
                .Where(m => m.Type == ContainerMountType.Volume && !string.IsNullOrEmpty(m.Source))
                .ToList();

            foreach (var volume in volumes)
            {
                var name = volume.Source!;

                if (name.EndsWith("-test")) continue;
                
                resource.Annotations.Remove(volume);
                resource.Annotations.Add(new ContainerMountAnnotation(
                    $"{name}-test", volume.Target, ContainerMountType.Volume, volume.IsReadOnly));
            }
        }
    }
}