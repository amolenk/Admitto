using Amolenk.Admitto.Module.Registrations.Application.ModuleEvents;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
namespace Amolenk.Admitto.Module.Registrations.Application.Messaging;

public class RegistrationsMessagePolicy : MessagePolicy
{
    public RegistrationsMessagePolicy()
    {
        Configure<AttendeeRegisteredDomainEvent>()
            .PublishIntegrationEvent(e => new AttendeeRegisteredIntegrationEvent(
                e.TeamId.Value,
                e.TicketedEventId.Value,
                e.RegistrationId.Value,
                e.RecipientEmail.Value,
                e.FirstName.Value,
                e.LastName.Value,
                e.Tickets.Select(t => new TicketTypeItem(t.Slug, t.Name)).ToList()));

        Configure<RegistrationCancelledDomainEvent>()
            .PublishIntegrationEvent(e => new RegistrationCancelledIntegrationEvent(
                e.TeamId.Value,
                e.TicketedEventId.Value,
                e.RegistrationId.Value,
                e.Email.Value,
                e.Reason.ToString()));

        Configure<RegistrationReconfirmedDomainEvent>()
            .PublishIntegrationEvent(e => new RegistrationReconfirmedIntegrationEvent(
                e.TeamId.Value,
                e.TicketedEventId.Value,
                e.RegistrationId.Value,
                e.Email.Value,
                e.ReconfirmedAt));

        Configure<CouponCreatedDomainEvent>()
            .PublishModuleEvent(e => new CouponCreatedModuleEvent
            {
                CouponId = e.CouponId.Value,
                TicketedEventId = e.TicketedEventId.Value,
                Email = e.Email.Value
            });

        Configure<TicketsChangedDomainEvent>()
            .PublishIntegrationEvent(e => new AttendeeTicketsChangedIntegrationEvent(
                e.TeamId.Value,
                e.TicketedEventId.Value,
                e.RegistrationId.Value,
                e.RecipientEmail.Value,
                e.FirstName.Value,
                e.LastName.Value,
                e.NewTickets.Select(t => new TicketTypeItem(t.Slug, t.Name)).ToList(),
                e.ChangedAt));

        Configure<TicketedEventStatusChangedDomainEvent>()
            .PublishIntegrationEvent(e => e.NewStatus switch
            {
                EventLifecycleStatus.Cancelled => new TicketedEventCancelled(
                    e.TeamId.Value,
                    e.TicketedEventId.Value,
                    e.Slug.Value),
                EventLifecycleStatus.Archived => new TicketedEventArchived(
                    e.TeamId.Value,
                    e.TicketedEventId.Value,
                    e.Slug.Value),
                _ => throw new InvalidOperationException(
                    $"Unexpected {nameof(EventLifecycleStatus)} '{e.NewStatus}' for " +
                    $"{nameof(TicketedEventStatusChangedDomainEvent)}.")
            });

        Configure<TicketedEventReconfirmPolicyChangedDomainEvent>()
            .PublishIntegrationEvent(e => new TicketedEventReconfirmPolicyChangedIntegrationEvent(
                e.TeamId.Value,
                e.TicketedEventId.Value,
                e.Policy is null
                    ? null
                    : new TicketedEventReconfirmPolicySnapshot(
                        e.Policy.OpensAt,
                        e.Policy.ClosesAt,
                        (int)e.Policy.Cadence.TotalDays)));
            
        Configure<TicketedEventTimeZoneChangedDomainEvent>()
            .PublishIntegrationEvent(e => new TicketedEventTimeZoneChangedIntegrationEvent(
                e.TeamId.Value,
                e.TicketedEventId.Value,
                e.TimeZone.Value));
    }
}
