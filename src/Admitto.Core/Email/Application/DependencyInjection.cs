using System.Reflection;
using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CancelBulkEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.EventHandlers;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmails;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.TriggerBulkEmailJob;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.CreateEmailSettings;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.DeleteEmailSettings;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.GetEmailSettings;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.UpdateEmailSettings;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplate;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplates;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ReconcileReconfirmationScheduling;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;
using Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Core.Email.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using Quartz;
using SendEmailHandlers = Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail.EventHandlers;

namespace Amolenk.Admitto.Core.Email.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddEmailModule(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var executingAssembly = Assembly.GetExecutingAssembly();

        // Command handlers
        services.AddScoped<CreateBulkEmailHandler>();
        services.AddScoped<ICommandHandler<CreateBulkEmailCommand, Guid>, CreateBulkEmailHandler>(
            sp => sp.GetRequiredService<CreateBulkEmailHandler>());
        services.AddScoped<CancelBulkEmailHandler>();
        services.AddScoped<ICommandHandler<CancelBulkEmailCommand>, CancelBulkEmailHandler>(
            sp => sp.GetRequiredService<CancelBulkEmailHandler>());
        services.AddScoped<TriggerBulkEmailJobHandler>();
        services.AddScoped<ICommandHandler<TriggerBulkEmailJobCommand>, TriggerBulkEmailJobHandler>(
            sp => sp.GetRequiredService<TriggerBulkEmailJobHandler>());
        services.AddScoped<CreateEmailSettingsHandler>();
        services.AddScoped<ICommandHandler<CreateEmailSettingsCommand>, CreateEmailSettingsHandler>(
            sp => sp.GetRequiredService<CreateEmailSettingsHandler>());
        services.AddScoped<UpdateEmailSettingsHandler>();
        services.AddScoped<ICommandHandler<UpdateEmailSettingsCommand>, UpdateEmailSettingsHandler>(
            sp => sp.GetRequiredService<UpdateEmailSettingsHandler>());
        services.AddScoped<DeleteEmailSettingsHandler>();
        services.AddScoped<ICommandHandler<DeleteEmailSettingsCommand>, DeleteEmailSettingsHandler>(
            sp => sp.GetRequiredService<DeleteEmailSettingsHandler>());
        services.AddScoped<SendTestEmailHandler>();
        services.AddScoped<ICommandHandler<SendTestEmailCommand>, SendTestEmailHandler>(
            sp => sp.GetRequiredService<SendTestEmailHandler>());
        services.AddScoped<CreateEmailTemplateHandler>();
        services.AddScoped<ICommandHandler<CreateEmailTemplateCommand, Guid>, CreateEmailTemplateHandler>(
            sp => sp.GetRequiredService<CreateEmailTemplateHandler>());
        services.AddScoped<UpdateEmailTemplateHandler>();
        services.AddScoped<ICommandHandler<UpdateEmailTemplateCommand>, UpdateEmailTemplateHandler>(
            sp => sp.GetRequiredService<UpdateEmailTemplateHandler>());
        services.AddScoped<DeleteEmailTemplateHandler>();
        services.AddScoped<ICommandHandler<DeleteEmailTemplateCommand>, DeleteEmailTemplateHandler>(
            sp => sp.GetRequiredService<DeleteEmailTemplateHandler>());
        services.AddScoped<TestSendEmailTemplateHandler>();
        services.AddScoped<ICommandHandler<TestSendEmailTemplateCommand>, TestSendEmailTemplateHandler>(
            sp => sp.GetRequiredService<TestSendEmailTemplateHandler>());
        services.AddScoped<ScheduleReconfirmationsHandler>();
        services.AddScoped<ICommandHandler<ScheduleReconfirmationsCommand>, ScheduleReconfirmationsHandler>(
            sp => sp.GetRequiredService<ScheduleReconfirmationsHandler>());
        services.AddScoped<ReconcileReconfirmationSchedulingHandler>();
        services.AddScoped<ICommandHandler<ReconcileReconfirmationSchedulingCommand>, ReconcileReconfirmationSchedulingHandler>(
            sp => sp.GetRequiredService<ReconcileReconfirmationSchedulingHandler>());
        services.AddScoped<SendEmailHandler>();
        services.AddScoped<ICommandHandler<SendEmailCommand>, SendEmailHandler>(
            sp => sp.GetRequiredService<SendEmailHandler>());

        // Query handlers
        services.AddScoped<GetAttendeeEmailsHandler>();
        services.AddScoped<IQueryHandler<GetAttendeeEmailsQuery, IReadOnlyList<AttendeeEmailLogItemDto>>, GetAttendeeEmailsHandler>(
            sp => sp.GetRequiredService<GetAttendeeEmailsHandler>());
        services.AddScoped<GetBulkEmailHandler>();
        services.AddScoped<IQueryHandler<GetBulkEmailQuery, BulkEmailJobDetailDto?>, GetBulkEmailHandler>(
            sp => sp.GetRequiredService<GetBulkEmailHandler>());
        services.AddScoped<GetBulkEmailsHandler>();
        services.AddScoped<IQueryHandler<GetBulkEmailsQuery, IReadOnlyList<BulkEmailListItemDto>>, GetBulkEmailsHandler>(
            sp => sp.GetRequiredService<GetBulkEmailsHandler>());
        services.AddScoped<GetEmailSettingsHandler>();
        services.AddScoped<IQueryHandler<GetEmailSettingsQuery, EmailSettingsDto?>, GetEmailSettingsHandler>(
            sp => sp.GetRequiredService<GetEmailSettingsHandler>());
        services.AddScoped<GetEmailTemplateHandler>();
        services.AddScoped<IQueryHandler<GetEmailTemplateQuery, EmailTemplateDto?>, GetEmailTemplateHandler>(
            sp => sp.GetRequiredService<GetEmailTemplateHandler>());
        services.AddScoped<GetEmailTemplatesHandler>();
        services.AddScoped<IQueryHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplateListItemDto>>, GetEmailTemplatesHandler>(
            sp => sp.GetRequiredService<GetEmailTemplatesHandler>());
        services.AddScoped<PreviewEmailTemplateHandler>();
        services.AddScoped<IQueryHandler<PreviewEmailTemplateQuery, PreviewEmailTemplateDto>, PreviewEmailTemplateHandler>(
            sp => sp.GetRequiredService<PreviewEmailTemplateHandler>());

        // Domain event handlers
        services.AddScoped<IDomainEventHandler<BulkEmailJobRequestedDomainEvent>, BulkEmailJobRequestedDomainEventHandler>();

        // Integration event handlers
        services.AddScoped<IIntegrationEventHandler<AttendeeRegisteredIntegrationEvent>, SendEmailHandlers.AttendeeRegisteredIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<AttendeeTicketsChangedIntegrationEvent>, SendEmailHandlers.AttendeeTicketsChangedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<OtpCodeRequestedIntegrationEvent>, SendEmailHandlers.OtpCodeRequestedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<RegistrationCancelledIntegrationEvent>, SendEmailHandlers.RegistrationCancelledIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketedEventArchivedIntegrationEvent>, TicketedEventArchivedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketedEventCancelledIntegrationEvent>, TicketedEventCancelledIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketedEventCreatedIntegrationEvent>, TicketedEventCreatedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketedEventReconfirmPolicyChangedIntegrationEvent>, TicketedEventReconfirmPolicyChangedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketedEventTimeZoneChangedIntegrationEvent>, TicketedEventTimeZoneChangedIntegrationEventHandler>();

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
        builder.AddCommand<TriggerBulkEmailJobCommand>();

        // Inbound integration events (published by Registrations module)
        builder.AddIntegrationEvent<AttendeeRegisteredIntegrationEvent>();
        builder.AddIntegrationEvent<AttendeeTicketsChangedIntegrationEvent>();
        builder.AddIntegrationEvent<OtpCodeRequestedIntegrationEvent>();
        builder.AddIntegrationEvent<RegistrationCancelledIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventArchivedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventCancelledIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventCreatedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventReconfirmPolicyChangedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventTimeZoneChangedIntegrationEvent>();
    }
}
