using System.Reflection;
using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Application.Common.Cryptography;
using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Registrations.Application.UseCases;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.CreateCoupon;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.GetCouponDetails;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.ListCoupons;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.RevokeCoupon;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.RequestOtp;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.VerifyOtp;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.QueryRegistrations;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee;
using GetRegistrationsHandler = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations.GetRegistrationsHandler;
using GetRegistrationsQuery = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations.GetRegistrationsQuery;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReleaseTickets;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.GetPublicTicketTypes;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.GetTicketTypes;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.CancelTicketedEvent;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventDetails;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetActiveReconfirmTriggerSpecs;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetReconfirmTriggerSpec;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ProjectEventStatus.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Cryptography;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using ReleaseTicketsHandlers = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReleaseTickets.EventHandlers;
using WriteActivityLogHandlers = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;
using GetRegistrationsItemDto = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations.RegistrationListItemDto;

namespace Amolenk.Admitto.Core.Registrations.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddRegistrationsModule(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var executingAssembly = Assembly.GetExecutingAssembly();

        // Command handlers
        services.AddScoped<CreateCouponHandler>();
        services.AddScoped<ICommandHandler<CreateCouponCommand, Guid>, CreateCouponHandler>(
            sp => sp.GetRequiredService<CreateCouponHandler>());
        services.AddScoped<RevokeCouponHandler>();
        services.AddScoped<ICommandHandler<RevokeCouponCommand>, RevokeCouponHandler>(
            sp => sp.GetRequiredService<RevokeCouponHandler>());
        services.AddScoped<RequestOtpHandler>();
        services.AddScoped<ICommandHandler<RequestOtpCommand>, RequestOtpHandler>(
            sp => sp.GetRequiredService<RequestOtpHandler>());
        services.AddScoped<VerifyOtpHandler>();
        services.AddScoped<ICommandHandler<VerifyOtpCommand, string>, VerifyOtpHandler>(
            sp => sp.GetRequiredService<VerifyOtpHandler>());
        services.AddScoped<CancelRegistrationHandler>();
        services.AddScoped<ICommandHandler<CancelRegistrationCommand>, CancelRegistrationHandler>(
            sp => sp.GetRequiredService<CancelRegistrationHandler>());
        services.AddScoped<ChangeAttendeeTicketsHandler>();
        services.AddScoped<ICommandHandler<ChangeAttendeeTicketsCommand>, ChangeAttendeeTicketsHandler>(
            sp => sp.GetRequiredService<ChangeAttendeeTicketsHandler>());
        services.AddScoped<RegisterAttendeeHandler>();
        services.AddScoped<ICommandHandler<RegisterAttendeeCommand, Guid>, RegisterAttendeeHandler>(
            sp => sp.GetRequiredService<RegisterAttendeeHandler>());
        services.AddScoped<ReleaseTicketsHandler>();
        services.AddScoped<ICommandHandler<ReleaseTicketsCommand>, ReleaseTicketsHandler>(
            sp => sp.GetRequiredService<ReleaseTicketsHandler>());
        services.AddScoped<WriteActivityLogHandler>();
        services.AddScoped<ICommandHandler<WriteActivityLogCommand>, WriteActivityLogHandler>(
            sp => sp.GetRequiredService<WriteActivityLogHandler>());
        services.AddScoped<AddTicketTypeHandler>();
        services.AddScoped<ICommandHandler<AddTicketTypeCommand>, AddTicketTypeHandler>(
            sp => sp.GetRequiredService<AddTicketTypeHandler>());
        services.AddScoped<CancelTicketTypeHandler>();
        services.AddScoped<ICommandHandler<CancelTicketTypeCommand>, CancelTicketTypeHandler>(
            sp => sp.GetRequiredService<CancelTicketTypeHandler>());
        services.AddScoped<UpdateTicketTypeHandler>();
        services.AddScoped<ICommandHandler<UpdateTicketTypeCommand>, UpdateTicketTypeHandler>(
            sp => sp.GetRequiredService<UpdateTicketTypeHandler>());
        services.AddScoped<ArchiveTicketedEventHandler>();
        services.AddScoped<ICommandHandler<ArchiveTicketedEventCommand>, ArchiveTicketedEventHandler>(
            sp => sp.GetRequiredService<ArchiveTicketedEventHandler>());
        services.AddScoped<CancelTicketedEventHandler>();
        services.AddScoped<ICommandHandler<CancelTicketedEventCommand>, CancelTicketedEventHandler>(
            sp => sp.GetRequiredService<CancelTicketedEventHandler>());
        services.AddScoped<ConfigureCancellationPolicyHandler>();
        services.AddScoped<ICommandHandler<ConfigureCancellationPolicyCommand>, ConfigureCancellationPolicyHandler>(
            sp => sp.GetRequiredService<ConfigureCancellationPolicyHandler>());
        services.AddScoped<ConfigureReconfirmPolicyHandler>();
        services.AddScoped<ICommandHandler<ConfigureReconfirmPolicyCommand>, ConfigureReconfirmPolicyHandler>(
            sp => sp.GetRequiredService<ConfigureReconfirmPolicyHandler>());
        services.AddScoped<ConfigureRegistrationPolicyHandler>();
        services.AddScoped<ICommandHandler<ConfigureRegistrationPolicyCommand>, ConfigureRegistrationPolicyHandler>(
            sp => sp.GetRequiredService<ConfigureRegistrationPolicyHandler>());
        services.AddScoped<UpdateAdditionalDetailSchemaHandler>();
        services.AddScoped<ICommandHandler<UpdateAdditionalDetailSchemaCommand>, UpdateAdditionalDetailSchemaHandler>(
            sp => sp.GetRequiredService<UpdateAdditionalDetailSchemaHandler>());
        services.AddScoped<UpdateTicketedEventDetailsHandler>();
        services.AddScoped<ICommandHandler<UpdateTicketedEventDetailsCommand>, UpdateTicketedEventDetailsHandler>(
            sp => sp.GetRequiredService<UpdateTicketedEventDetailsHandler>());
        services.AddScoped<UpdateTicketedEventTimeZoneHandler>();
        services.AddScoped<ICommandHandler<UpdateTicketedEventTimeZoneCommand>, UpdateTicketedEventTimeZoneHandler>(
            sp => sp.GetRequiredService<UpdateTicketedEventTimeZoneHandler>());

        // Query handlers
        services.AddScoped<GetCouponDetailsHandler>();
        services.AddScoped<IQueryHandler<GetCouponDetailsQuery, CouponDetailsDto>, GetCouponDetailsHandler>(
            sp => sp.GetRequiredService<GetCouponDetailsHandler>());
        services.AddScoped<ListCouponsHandler>();
        services.AddScoped<IQueryHandler<ListCouponsQuery, ListCouponsResult>, ListCouponsHandler>(
            sp => sp.GetRequiredService<ListCouponsHandler>());
        services.AddScoped<GetRegistrationDetailsHandler>();
        services.AddScoped<IQueryHandler<GetRegistrationDetailsQuery, RegistrationDetailDto?>, GetRegistrationDetailsHandler>(
            sp => sp.GetRequiredService<GetRegistrationDetailsHandler>());
        services.AddScoped<GetRegistrationsHandler>();
        services.AddScoped<IQueryHandler<GetRegistrationsQuery, IReadOnlyList<GetRegistrationsItemDto>?>, GetRegistrationsHandler>(
            sp => sp.GetRequiredService<GetRegistrationsHandler>());
        services.AddScoped<QueryRegistrationsHandler>();
        services.AddScoped<IQueryHandler<QueryRegistrationsQuery, IReadOnlyList<RegistrationListItemDto>>, QueryRegistrationsHandler>(
            sp => sp.GetRequiredService<QueryRegistrationsHandler>());
        services.AddScoped<GetPublicTicketTypesHandler>();
        services.AddScoped<IQueryHandler<GetPublicTicketTypesQuery, IReadOnlyList<PublicTicketTypeDto>>, GetPublicTicketTypesHandler>(
            sp => sp.GetRequiredService<GetPublicTicketTypesHandler>());
        services.AddScoped<GetTicketTypesHandler>();
        services.AddScoped<IQueryHandler<GetTicketTypesQuery, IReadOnlyList<TicketTypeDto>>, GetTicketTypesHandler>(
            sp => sp.GetRequiredService<GetTicketTypesHandler>());
        services.AddScoped<GetTicketedEventDetailsHandler>();
        services.AddScoped<IQueryHandler<GetTicketedEventDetailsQuery, TicketedEventDetailsDto?>, GetTicketedEventDetailsHandler>(
            sp => sp.GetRequiredService<GetTicketedEventDetailsHandler>());
        services.AddScoped<GetTicketedEventsHandler>();
        services.AddScoped<IQueryHandler<GetTicketedEventsQuery, IReadOnlyList<TicketedEventListItemDto>>, GetTicketedEventsHandler>(
            sp => sp.GetRequiredService<GetTicketedEventsHandler>());
        services.AddScoped<GetActiveReconfirmTriggerSpecsHandler>();
        services.AddScoped<IQueryHandler<GetActiveReconfirmTriggerSpecsQuery, IReadOnlyList<ReconfirmTriggerSpecDto>>, GetActiveReconfirmTriggerSpecsHandler>(
            sp => sp.GetRequiredService<GetActiveReconfirmTriggerSpecsHandler>());
        services.AddScoped<GetReconfirmTriggerSpecHandler>();
        services.AddScoped<IQueryHandler<GetReconfirmTriggerSpecQuery, ReconfirmTriggerSpecDto?>, GetReconfirmTriggerSpecHandler>(
            sp => sp.GetRequiredService<GetReconfirmTriggerSpecHandler>());
        services.AddScoped<GetTicketedEventEmailContextHandler>();
        services.AddScoped<IQueryHandler<GetTicketedEventEmailContextQuery, TicketedEventEmailContextDto>, GetTicketedEventEmailContextHandler>(
            sp => sp.GetRequiredService<GetTicketedEventEmailContextHandler>());

        // Domain event handlers
        services.AddScoped<IDomainEventHandler<RegistrationCancelledDomainEvent>, ReleaseTicketsHandlers.RegistrationCancelledDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<AttendeeRegisteredDomainEvent>, AttendeeRegisteredDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<RegistrationCancelledDomainEvent>, WriteActivityLogHandlers.RegistrationCancelledDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<RegistrationReconfirmedDomainEvent>, RegistrationReconfirmedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<TicketsChangedDomainEvent>, TicketsChangedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<TicketedEventStatusChangedDomainEvent>, TicketedEventStatusChangedDomainEventHandler>();

        services.AddValidatorsFromAssembly(executingAssembly);

        services.AddScoped<IRegistrationsFacade, RegistrationsFacade>();

        services.AddMemoryCache();
        services.AddScoped<IEventSigningKeyProvider, EventSigningKeyProvider>();
        services.AddScoped<RegistrationSigner>();

        services.Configure<VerificationTokenOptions>(
            configuration.GetSection(VerificationTokenOptions.SectionName));
        services.AddScoped<IVerificationTokenService, HmacVerificationTokenService>();

        services.Configure<OtpOptions>(
            configuration.GetSection(OtpOptions.SectionName));

        return builder;
    }

    public static IHostApplicationBuilder AddRegistrationsModuleWorker(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IIntegrationEventHandler<TicketedEventCreationRequestedIntegrationEvent>,
            TicketedEventCreationRequestedIntegrationEventHandler>();

        return builder;
    }

    public static void AddRegistrationsMessageTypes(this MessageTypeRegistryBuilder builder)
    {
        builder.AddIntegrationEvent<AttendeeRegisteredIntegrationEvent>();
        builder.AddIntegrationEvent<AttendeeTicketsChangedIntegrationEvent>();
        builder.AddIntegrationEvent<OtpCodeRequestedIntegrationEvent>();
        builder.AddIntegrationEvent<RegistrationCancelledIntegrationEvent>();
        builder.AddIntegrationEvent<RegistrationReconfirmedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventArchivedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventCancelledIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventCreatedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventCreationRejectedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventReconfirmPolicyChangedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventTimeZoneChangedIntegrationEvent>();
    }
}