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
                e.RecipientName));

        Configure<CouponCreatedDomainEvent>()
            .PublishModuleEvent(e => new CouponCreatedModuleEvent
            {
                CouponId = e.CouponId.Value,
                TicketedEventId = e.TicketedEventId.Value,
                Email = e.Email.Value
            });

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
    }
}
