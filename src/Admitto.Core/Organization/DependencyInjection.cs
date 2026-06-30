using System.Reflection;
using Amolenk.Admitto.Core.Organization;
using Amolenk.Admitto.Core.Organization.Application;
using Amolenk.Admitto.Core.Organization.Application.ExternalUsers;
using Amolenk.Admitto.Core.Organization.Application.Jobs;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.BootstrapAdminUser;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.Keycloak;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class OrganizationModuleExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddOrganizationModule(bool includeWorkerHandlers = false)
        {
            var services = builder.Services;
            var assembly = Assembly.GetExecutingAssembly();

            // Command handlers
            services.AddCommandHandlersFromAssembly(
                assembly,
                OrganizationModule.NamespacePrefix,
                includeWorkerHandlers);

            // Query handlers
            services.AddQueryHandlersFromAssembly(assembly, OrganizationModule.NamespacePrefix);

            // Domain event handlers
            services.AddDomainEventHandlersFromAssembly(assembly, OrganizationModule.NamespacePrefix);

            // Validators
            services.AddValidatorsFromAssembly(assembly, OrganizationModule.NamespacePrefix);

            // Facade
            services.AddScoped<IOrganizationFacade, OrganizationFacade>();

            // Infrastructure
            builder.AddModuleDatabaseServices<IOrganizationWriteStore, OrganizationDbContext>(
                OrganizationModule.Key);

            services.AddKeyedScoped<IPostgresExceptionMapping, PostgresExceptionMapping>(
                OrganizationModule.Key);

            return builder;
        }

        public IHostApplicationBuilder AddOrganizationModuleWorker()
        {
            builder.AddOrganizationModule(includeWorkerHandlers: true);

            var services = builder.Services;
            var assembly = Assembly.GetExecutingAssembly();

            // Integration event handlers
            services.AddIntegrationEventHandlersFromAssembly(assembly, OrganizationModule.NamespacePrefix);

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

            builder.AddKeycloakUserManagementServices();

            // Bootstrap admin (only when configured)
            var bootstrapEmail = builder.Configuration[$"{BootstrapAdminUserOptions.SectionName}:EmailAddress"];
            if (!string.IsNullOrWhiteSpace(bootstrapEmail))
            {
                services.Configure<BootstrapAdminUserOptions>(
                    builder.Configuration.GetSection(BootstrapAdminUserOptions.SectionName));
                services.AddHostedService<BootstrapAdminUserInitializer>();
            }

            return builder;
        }

        private void AddKeycloakUserManagementServices()
        {
            var services = builder.Services;

            services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
            services.AddOptions<KeycloakOptions>()
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddTransient<KeycloakAccessTokenHandler>();

            var settings = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>();
            services.AddHttpClient<IExternalUserDirectory, KeycloakUserManagementService>(client =>
                    client.BaseAddress = new Uri(settings!.Authority))
                .AddHttpMessageHandler<KeycloakAccessTokenHandler>();
        }
    }
}
