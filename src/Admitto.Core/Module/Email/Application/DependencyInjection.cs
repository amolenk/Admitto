using System.Reflection;
using Amolenk.Admitto.Core.Module.Email.Application.Jobs;
using Amolenk.Admitto.Core.Module.Email.Application.Messaging;
using Amolenk.Admitto.Core.Module.Email.Application.Sending;
using Amolenk.Admitto.Core.Module.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Module.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Module.Email.Application.Templating;
using Amolenk.Admitto.Core.Module.Email.Application.UseCases.Reconfirmations.ReconcileReconfirmationScheduling;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Amolenk.Admitto.Core.Module.Email.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddEmailApplicationServices(
        this IHostApplicationBuilder builder,
        HostCapability capabilities = HostCapability.None)
    {
        var services = builder.Services;
        var executingAssembly = Assembly.GetExecutingAssembly();

        services.AddCommandHandlersFromAssembly(executingAssembly, capabilities);
        services.AddDomainEventHandlersFromAssembly(executingAssembly);
        services.AddModuleEventHandlersFromAssembly(executingAssembly, capabilities);
        services.AddIntegrationEventHandlersFromAssembly(executingAssembly, EmailModuleKey.Value, capabilities);
        services.AddQueryHandlersFromAssembly(executingAssembly, capabilities);
        services.AddValidatorsFromAssembly(executingAssembly);

        services.AddKeyedSingleton<IMessagePolicy, EmailMessagePolicy>(EmailModuleKey.Value);

        services.AddScoped<IEffectiveEmailSettingsResolver, EffectiveEmailSettingsResolver>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IBulkEmailRecipientResolver, BulkEmailRecipientResolver>();
        services.AddSingleton<IEmailRenderer, ScribanEmailRenderer>();

        services.Configure<BulkEmailOptions>(
            builder.Configuration.GetSection(BulkEmailOptions.SectionName));

        if (capabilities.HasFlag(HostCapability.Jobs) && capabilities.HasFlag(HostCapability.Email))
        {
            services.AddQuartz(options =>
            {
                // RequestReconfirmationsJob is registered statically; per-event
                // triggers are added/replaced/removed by the
                // ScheduleReconfirmations use case in response to integration
                // events.
                options.AddJob<RequestReconfirmationsJob>(c => c
                    .StoreDurably()
                    .WithIdentity(RequestReconfirmationsJob.Name));

                // SendBulkEmailJob is scheduled dynamically per-bulk-job by
                // TriggerBulkEmailJobHandler so each bulk job gets a unique
                // JobKey (D10: per-job concurrency isolation).
            });

            services.AddHostedService<ReconcileReconfirmationSchedulingStartupService>();
        }

        return builder;
    }
}
