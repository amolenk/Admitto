using System.Reflection;
using Amolenk.Admitto.Core.Organization.Application.Jobs;
using Amolenk.Admitto.Core.Organization.Application.Mapping;
using Amolenk.Admitto.Core.Organization.Application.Messaging;
using Amolenk.Admitto.Core.Organization.Application.UseCases;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Amolenk.Admitto.Core.Organization.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddOrganizationApplicationServices(
        this IHostApplicationBuilder builder,
        HostCapability capabilities = HostCapability.None)
    {
        var services = builder.Services;
        var executingAssembly = Assembly.GetExecutingAssembly();

        services.AddCommandHandlersFromAssembly(executingAssembly, capabilities);
        services.AddDomainEventHandlersFromAssembly(executingAssembly);
        services.AddModuleEventHandlersFromAssembly(executingAssembly, capabilities);
        services.AddIntegrationEventHandlersFromAssembly(executingAssembly, OrganizationModuleKey.Value, capabilities);
        services.AddQueryHandlersFromAssembly(executingAssembly, capabilities);
        services.AddValidatorsFromAssembly(executingAssembly);
        
        services.AddScoped<OrganizationFacade>();
        services.AddScoped<IOrganizationFacade>(sp =>
        {
            // TODO Options?
            if (builder.Configuration["ORGANIZATION__CACHING__ENABLED"] != "true")
                return sp.GetRequiredService<OrganizationFacade>();

            var inner = sp.GetRequiredService<OrganizationFacade>();
            return new CachingOrganizationFacade(inner);
        });

        services.AddKeyedSingleton<IMessagePolicy, OrganizationMessagePolicy>(
            OrganizationModuleKey.Value);

        if (capabilities.HasFlag(HostCapability.Jobs))
        {
            builder.AddOrganizationJobs();
        }

        return builder;
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
