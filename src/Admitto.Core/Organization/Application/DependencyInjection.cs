using System.Reflection;
using Amolenk.Admitto.Core.Organization.Application.Jobs;
using Amolenk.Admitto.Core.Organization.Application.UseCases;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Amolenk.Admitto.Core.Organization.Application;

public static class DependencyInjection
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

        return builder;
    }

    public static IHostApplicationBuilder AddOrganizationModuleWorker(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Integration event handlers
        services.AddIntegrationEventHandlersFromAssembly(
            Assembly.GetExecutingAssembly(),
            "Amolenk.Admitto.Core.Organization");

        // Worker-only interface mapping — concrete already registered by AddOrganizationModule scan
        services.AddScoped<ICommandHandler<RegisterExternalUserCommand>, RegisterExternalUserHandler>(
            sp => sp.GetRequiredService<RegisterExternalUserHandler>());

        builder.AddOrganizationJobs();

        return builder;
    }

    public static void AddOrganizationMessageTypes(this MessageTypeRegistryBuilder builder)
    {
        builder.AddFromAssembly(Assembly.GetExecutingAssembly(), "Amolenk.Admitto.Core.Organization");
    }

    private static void AddOrganizationJobs(this IHostApplicationBuilder builder)
    {
        builder.Services.AddQuartz(options =>
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

        builder.Services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });
    }
}
