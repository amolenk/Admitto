using System.Reflection;
using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.TriggerBulkEmailJob;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ReconcileReconfirmationScheduling;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;
using Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Amolenk.Admitto.Core.Email.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddEmailModule(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var executingAssembly = Assembly.GetExecutingAssembly();

        // Quartz infrastructure is needed by handlers that schedule/trigger jobs
        // (ScheduleReconfirmationsHandler, TriggerBulkEmailJobHandler). Job
        // registrations and the hosted service live in AddEmailModuleWorker.
        services.AddQuartz();

        // Command handlers
        services.AddConcreteCommandHandlersFromAssembly(executingAssembly, "Amolenk.Admitto.Core.Email");

        // Query handlers
        services.AddConcreteQueryHandlersFromAssembly(executingAssembly, "Amolenk.Admitto.Core.Email");

        // Domain event handlers
        services.AddDomainEventHandlersFromAssembly(executingAssembly, "Amolenk.Admitto.Core.Email");

        services.AddValidatorsFromAssembly(executingAssembly);

        services.AddScoped<IEffectiveEmailSettingsResolver, EffectiveEmailSettingsResolver>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IBulkEmailRecipientResolver, BulkEmailRecipientResolver>();
        services.AddSingleton<IEmailRenderer, ScribanEmailRenderer>();

        services.Configure<BulkEmailOptions>(
            builder.Configuration.GetSection(BulkEmailOptions.SectionName));

        return builder;
    }

    public static IHostApplicationBuilder AddEmailModuleWorker(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var executingAssembly = Assembly.GetExecutingAssembly();

        // Integration event handlers
        services.AddIntegrationEventHandlersFromAssembly(executingAssembly, "Amolenk.Admitto.Core.Email");

        // Worker-only interface mappings — concretes already registered by AddEmailModule scan;
        // integration event handlers and the queue dispatcher resolve these by interface.
        services.AddScoped<ICommandHandler<SendEmailCommand>, SendEmailHandler>(
            sp => sp.GetRequiredService<SendEmailHandler>());
        services.AddScoped<ICommandHandler<ScheduleReconfirmationsCommand>, ScheduleReconfirmationsHandler>(
            sp => sp.GetRequiredService<ScheduleReconfirmationsHandler>());
        services.AddScoped<ICommandHandler<TriggerBulkEmailJobCommand>, TriggerBulkEmailJobHandler>(
            sp => sp.GetRequiredService<TriggerBulkEmailJobHandler>());

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

        return builder;
    }

    public static void AddEmailMessageTypes(this MessageTypeRegistryBuilder builder)
    {
        builder.AddFromAssembly(Assembly.GetExecutingAssembly(), "Amolenk.Admitto.Core.Email");
    }
}
