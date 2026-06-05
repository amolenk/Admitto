using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.Messaging;

internal sealed class RegistrationsIntegrationEventPublisher(
    [FromKeyedServices(RegistrationsModule.Key)] IOutbox outbox)
    : IDomainEventHandler<AttendeeRegisteredDomainEvent>,
      IDomainEventHandler<CouponCreatedDomainEvent>,
      IDomainEventHandler<OtpCodeRequestedDomainEvent>,
      IDomainEventHandler<RegistrationCancelledDomainEvent>,
      IDomainEventHandler<RegistrationReconfirmedDomainEvent>,
      IDomainEventHandler<TicketedEventCreatedDomainEvent>,
      IDomainEventHandler<TicketedEventReconfirmPolicyChangedDomainEvent>,
      IDomainEventHandler<TicketedEventStatusChangedDomainEvent>,
      IDomainEventHandler<TicketedEventTimeZoneChangedDomainEvent>,
      IDomainEventHandler<TicketsChangedDomainEvent>,
      IDomainEventHandler<WaitlistCouponIssuedDomainEvent>
{
    public ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new AttendeeRegisteredIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.RegistrationId.Value,
            domainEvent.RecipientEmail.Value,
            domainEvent.FirstName.Value,
            domainEvent.LastName.Value,
            domainEvent.Tickets.Select(t => new TicketTypeItem(t.Id.Value, t.Name.Value)).ToList()));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(CouponCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new CouponCreatedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.Email.Value,
            domainEvent.Code.Value.ToString()));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(OtpCodeRequestedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new OtpCodeRequestedIntegrationEvent(
            domainEvent.OtpCodeId.Value,
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.EventName.Value,
            domainEvent.RecipientEmail.Value,
            domainEvent.PlainCode));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(RegistrationCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new RegistrationCancelledIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.RegistrationId.Value,
            domainEvent.Email.Value,
            domainEvent.Reason.ToString()));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(RegistrationReconfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new RegistrationReconfirmedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.RegistrationId.Value,
            domainEvent.Email.Value,
            domainEvent.ReconfirmedAt));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(TicketedEventCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TicketedEventCreatedIntegrationEvent(
            domainEvent.CreationRequestId.Value,
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.TimeZone.Value));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(
        TicketedEventReconfirmPolicyChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TicketedEventReconfirmPolicyChangedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.Policy is null
                ? null
                : new TicketedEventReconfirmPolicySnapshot(
                    domainEvent.Policy.OpensAt,
                    domainEvent.Policy.ClosesAt,
                    (int)domainEvent.Policy.Cadence.TotalHours,
                    (int)domainEvent.Policy.MinEmailInterval.TotalHours)));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(TicketedEventStatusChangedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TicketedEventArchivedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(
        TicketedEventTimeZoneChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TicketedEventTimeZoneChangedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.TimeZone.Value));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(TicketsChangedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new AttendeeTicketsChangedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.RegistrationId.Value,
            domainEvent.RecipientEmail.Value,
            domainEvent.FirstName.Value,
            domainEvent.LastName.Value,
            domainEvent.NewTickets.Select(t => new TicketTypeItem(t.Id.Value, t.Name.Value)).ToList(),
            domainEvent.ChangedAt));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(WaitlistCouponIssuedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new WaitlistCouponIssuedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.RecipientEmail.Value,
            domainEvent.CouponCode.Value.ToString(),
            domainEvent.TicketTypeName,
            domainEvent.ExpiresAt));

        return ValueTask.CompletedTask;
    }
}
