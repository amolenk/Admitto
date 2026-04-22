using System.Reflection;
using Amolenk.Admitto.Module.Organization.Application.Jobs;
using Amolenk.Admitto.Module.Organization.Application.Mapping;
using Amolenk.Admitto.Module.Organization.Application.Messaging;
using Amolenk.Admitto.Module.Organization.Application.UseCases;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Amolenk.Admitto.Module.Organization.Application;

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
        services.AddModuleEventHandlersFromAssembly(executingAssembly);
        services.AddIntegrationEventHandlersFromAssembly(executingAssembly, OrganizationModuleKey.Value);
        services.AddQueryHandlersFromAssembly(executingAssembly);
        services.AddValidatorsFromAssembly(executingAssembly);
        
        services.AddScoped<OrganizationFacade>();
        services.AddScoped<IOrganizationFacade>(sp =>
        {
            // TODO Options?
            if (builder.Configuration["ORGANIZATION__CACHING__ENABLED"] != "true")
                return sp.GetRequiredService<OrganizationFacade>();

            var inner = sp.GetRequiredService<OrganizationFacade>();
            var memoryCache = sp.GetRequiredService<IMemoryCache>();
            return new CachingOrganizationFacade(inner, memoryCache);
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
