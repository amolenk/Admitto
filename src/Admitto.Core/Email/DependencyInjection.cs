using System.Reflection;
using Amolenk.Admitto.Core.Email.Application;
using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.TriggerBulkEmailJob;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ReconcileReconfirmationScheduling;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;
using Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Email.Infrastructure.Security;
using Amolenk.Admitto.Core.Email.Infrastructure.Sending;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class EmailModuleExtensions
{
    public static IHostApplicationBuilder AddEmailModule(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var assembly = Assembly.GetExecutingAssembly();

        // Quartz infrastructure is needed by handlers that schedule/trigger jobs
        // (ScheduleReconfirmationsHandler, TriggerBulkEmailJobHandler). Job
        // registrations and the hosted service live in AddEmailModuleWorker.
        services.AddQuartz();

        // Command handlers
        services.AddConcreteCommandHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Email");

        // Query handlers
        services.AddConcreteQueryHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Email");

        // Domain event handlers
        services.AddDomainEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Email");

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IEffectiveEmailSettingsResolver, EffectiveEmailSettingsResolver>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IBulkEmailRecipientResolver, BulkEmailRecipientResolver>();
        services.AddSingleton<IEmailRenderer, ScribanEmailRenderer>();

        services.Configure<BulkEmailOptions>(
            builder.Configuration.GetSection(BulkEmailOptions.SectionName));

        // Infrastructure
        builder.AddModuleDatabaseServices<IEmailWriteStore, EmailDbContext>(EmailModuleKey.Value);

        services.AddKeyedScoped<IPostgresExceptionMapping, EmailPostgresExceptionMapping>(
            EmailModuleKey.Value);

        // Shared Data Protection key ring persisted to the email schema so the API and Worker hosts
        // can decrypt secrets written by either side.
        services
            .AddDataProtection()
            .SetApplicationName("Admitto")
            .PersistKeysToDbContext<EmailDbContext>();

        services.AddSingleton<IProtectedSecret, ProtectedSecret>();
        services.AddSingleton<IEmailSender, MailKitEmailSender>();
        services.AddSingleton<IBulkSmtpSender, MailKitBulkSmtpSender>();

        return builder;
    }

    public static IHostApplicationBuilder AddEmailModuleWorker(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var assembly = Assembly.GetExecutingAssembly();

        // Integration event handlers
        services.AddIntegrationEventHandlersFromAssembly(assembly, "Amolenk.Admitto.Core.Email");

        // Worker-only interface mappings — concretes already registered by AddEmailModule scan;
        // integration event handlers and the queue dispatcher resolve these by interface.
        services.AddScoped<ICommandHandler<SendEmailCommand>, SendEmailHandler>(
            sp => sp.GetRequiredService<SendEmailHandler>());
        services.AddScoped<ICommandHandler<ScheduleReconfirmationsCommand>, ScheduleReconfirmationsHandler>(
            sp => sp.GetRequiredService<ScheduleReconfirmationsHandler>());
        services.AddScoped<ICommandHandler<TriggerBulkEmailJobCommand>, TriggerBulkEmailJobHandler>(
            sp => sp.GetRequiredService<TriggerBulkEmailJobHandler>());

        // Quartz job registrations (hosted service is started once by AddSharedInfrastructureQueueConsumer)
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
