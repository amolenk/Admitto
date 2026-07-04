using System.Text.Json;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.Projections.ActivityLog;

internal sealed class ActivityLogProjector(IRegistrationsReadStore readStore)
    : IDomainEventHandler<AttendeeRegisteredDomainEvent>,
      IDomainEventHandler<RegistrationReconfirmedDomainEvent>,
      IDomainEventHandler<RegistrationCancelledDomainEvent>,
      IDomainEventHandler<TicketsChangedDomainEvent>
{
    public ValueTask HandleAsync(
        AttendeeRegisteredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        AddEntry(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.RegistrationId,
            ActivityType.Registered,
            domainEvent.OccurredOn);

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(
        RegistrationReconfirmedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        AddEntry(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.RegistrationId,
            ActivityType.Reconfirmed,
            domainEvent.ReconfirmedAt);

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(
        RegistrationCancelledDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        AddEntry(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.RegistrationId,
            ActivityType.Cancelled,
            domainEvent.OccurredOn,
            domainEvent.Reason.ToString());

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(
        TicketsChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            from = domainEvent.OldTickets.Select(t => t.Id.Value).ToArray(),
            to = domainEvent.NewTickets.Select(t => t.Id.Value).ToArray()
        });

        AddEntry(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.RegistrationId,
            ActivityType.TicketsChanged,
            domainEvent.ChangedAt,
            metadata);

        return ValueTask.CompletedTask;
    }

    private void AddEntry(
        TeamId teamId,
        TicketedEventId eventId,
        RegistrationId registrationId,
        ActivityType activityType,
        DateTimeOffset occurredAt,
        string? metadata = null)
    {
        readStore.ActivityLog.Add(ActivityLogView.Create(
            teamId.Value,
            eventId.Value,
            registrationId.Value,
            activityType,
            occurredAt,
            metadata));
    }
}
