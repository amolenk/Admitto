using System.Reflection;
using Amolenk.Admitto.Core.Organization.Application;
using Amolenk.Admitto.Core.Organization.Application.Jobs;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Application.Services;
using Amolenk.Admitto.Core.Organization.Application.UseCases;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.Keycloak;
using Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.MicrosoftGraph;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Azure.Identity;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class OrganizationModuleExtensions
{
    public static IHostApplicationBuilder AddOrganizationModule(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var assembly = Assembly.GetExecutingAssembly();

        // Command handlers
        services.AddConcreteCommandHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Organization");

        // Query handlers
        services.AddConcreteQueryHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Organization");

        // Domain event handlers
        services.AddDomainEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Organization");

        // Validators
        services.AddValidatorsFromAssembly(assembly);

        // Facade
        services.AddScoped<OrganizationFacade>();
        services.AddScoped<IOrganizationFacade>(sp =>
        {
            if (builder.Configuration["ORGANIZATION__CACHING__ENABLED"] != "true")
                return sp.GetRequiredService<OrganizationFacade>();

            var inner = sp.GetRequiredService<OrganizationFacade>();
            return new CachingOrganizationFacade(inner);
        });

        // Infrastructure
        builder.AddModuleDatabaseServices<IOrganizationWriteStore, OrganizationDbContext>(
            OrganizationModuleKey.Value);

        services.AddKeyedScoped<IPostgresExceptionMapping, PostgresExceptionMapping>(
            OrganizationModuleKey.Value);

        return builder;
    }

    public static IHostApplicationBuilder AddOrganizationModuleWorker(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var assembly = Assembly.GetExecutingAssembly();

        // Integration event handlers
        services.AddIntegrationEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Organization");

        // Worker-only interface mapping — concrete already registered by AddOrganizationModule scan
        services.AddScoped<ICommandHandler<RegisterExternalUserCommand>, RegisterExternalUserHandler>(
            sp => sp.GetRequiredService<RegisterExternalUserHandler>());

        // Quartz job registrations (hosted service is started once by AddSharedInfrastructureQueueConsumer)
        services.AddQuartz(options =>
        {
            options.AddJob<DeprovisionUserIdpJob>(c => c
                .StoreDurably()
                .WithIdentity(DeprovisionUserIdpJob.Name));

            options.AddTrigger(t => t
                .ForJob(DeprovisionUserIdpJob.Name)
                .WithIdentity($"{DeprovisionUserIdpJob.Name}.trigger")
                .WithSimpleSchedule(s => s
                    .WithIntervalInHours(1)
                    .RepeatForever())
                .StartNow());

            options.AddJob<ExpireStaleEventCreationRequestsJob>(c => c
                .StoreDurably()
                .WithIdentity(ExpireStaleEventCreationRequestsJob.Name));

            options.AddTrigger(t => t
                .ForJob(ExpireStaleEventCreationRequestsJob.Name)
                .WithIdentity($"{ExpireStaleEventCreationRequestsJob.Name}.trigger")
                .WithSimpleSchedule(s => s
                    .WithIntervalInMinutes(15)
                    .RepeatForever())
                .StartNow());
        });

        return builder;
    }

    public static IHostApplicationBuilder AddOrganizationIdentityServices(this IHostApplicationBuilder builder)
    {
        if (builder.Configuration.GetSection(MicrosoftGraphOptions.SectionName).Exists())
            builder.AddMicrosoftGraphServices();
        else if (builder.Configuration.GetSection(KeycloakOptions.SectionName).Exists())
            builder.AddKeycloakServices();
        else
            throw new InvalidOperationException(
                "No user management service configured. Please configure either Microsoft Graph or Keycloak settings.");

        return builder;
    }

    public static void AddOrganizationMessageTypes(this MessageTypeRegistryBuilder builder)
    {
        builder.AddFromAssembly(Assembly.GetExecutingAssembly(), "Amolenk.Admitto.Core.Organization");
    }

    private static void AddMicrosoftGraphServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.Configure<MicrosoftGraphOptions>(
            builder.Configuration.GetSection(MicrosoftGraphOptions.SectionName));
        services.AddOptions<MicrosoftGraphOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

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

    private static void AddKeycloakServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
        services.AddOptions<KeycloakOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<KeycloakAccessTokenHandler>();

        var settings = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>();
        services.AddHttpClient<IExternalUserDirectory, KeycloakUserManagementService>(client =>
                client.BaseAddress = new Uri(settings!.Authority))
            .AddHttpMessageHandler<KeycloakAccessTokenHandler>();
    }
}
