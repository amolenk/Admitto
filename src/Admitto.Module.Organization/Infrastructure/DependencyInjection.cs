using Amolenk.Admitto.Module.Organization.Application;
using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Application.Services;
using Amolenk.Admitto.Module.Organization.Infrastructure.UserDirectories.Keycloak;
using Amolenk.Admitto.Module.Organization.Infrastructure.UserDirectories.MicrosoftGraph;
using Amolenk.Admitto.Module.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Shared.Infrastructure;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public IHostApplicationBuilder AddOrganizationInfrastructureServices()
        {
            builder.AddModuleDatabaseServices<IOrganizationWriteStore, OrganizationDbContext>(
                OrganizationModuleKey.Value);

            builder.Services.AddKeyedScoped<IPostgresExceptionMapping, PostgresExceptionMapping>(OrganizationModuleKey.Value);

            return builder;
        }

        public TBuilder AddOrganizationIdentityServices()
        {
            if (builder.Configuration.GetSection(MicrosoftGraphOptions.SectionName).Exists())
            {
                builder.AddMicrosoftGraphServices();
            }
            else if (builder.Configuration.GetSection(KeycloakOptions.SectionName).Exists())
            {
                builder.AddKeycloakServices();
            }
            else
            {
                throw new InvalidOperationException(
                    "No user management service configured. Please configure either Microsoft Graph or Keycloak settings.");
            }

            return builder;
        }

        private void AddMicrosoftGraphServices()
        {
            var services = builder.Services;

            services.Configure<MicrosoftGraphOptions>(
                builder.Configuration.GetSection(MicrosoftGraphOptions.SectionName));
            services.AddOptions<MicrosoftGraphOptions>()
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register Graph Service Client with client credentials authentication
            services.AddScoped<GraphServiceClient>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MicrosoftGraphOptions>>().Value;

                var credential = new ClientSecretCredential(
                    options.TenantId,
                    options.ClientId,
                    options.ClientSecret);

                return new GraphServiceClient(credential);
            });

            services.AddScoped<IExternalUserDirectory, MicrosoftGraphUserManagementService>();
        }

        private void AddKeycloakServices()
        {
            var services = builder.Services;

            services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
            services.AddOptions<KeycloakOptions>()
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register the auth handler
            services.AddSingleton<KeycloakAccessTokenHandler>();

            // Use Keycloak as the identity service
            var settings = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>();
            services.AddHttpClient<IExternalUserDirectory, KeycloakUserManagementService>(client =>
                {
                    // Use .NET Service Discovery to get Keycloak endpoint
                    client.BaseAddress = new Uri(settings!.Authority);
                })
                .AddHttpMessageHandler<KeycloakAccessTokenHandler>();
        }
    }
}