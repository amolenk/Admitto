using System.Text.Json;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.Projections.ActivityLog;

internal sealed class ActivityLogProjector(IRegistrationsReadStore readStore, IRegistrationsWriteStore writeStore)
    : IDomainEventHandler<AttendeeRegisteredDomainEvent>,
      IDomainEventHandler<RegistrationReconfirmedDomainEvent>,
      IDomainEventHandler<RegistrationCancelledDomainEvent>,
      IDomainEventHandler<TicketsChangedDomainEvent>,
      IDomainEventHandler<RegistrationCheckedInDomainEvent>,
      IDomainEventHandler<WaitlistCouponIssuedDomainEvent>,
      IDomainEventHandler<WaitlistCouponExpiredDomainEvent>,
      IDomainEventHandler<WaitlistEntryRemovedDomainEvent>
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
        // Confirmed and waitlisted selections are recorded as separate activity entries, since either can
        // change independently of the other (e.g. moving between waitlists leaves confirmed tickets as they were).
        if (!SameSelection(domainEvent.OldTickets, domainEvent.NewTickets))
        {
            AddEntry(
                domainEvent.TeamId,
                domainEvent.TicketedEventId,
                domainEvent.RegistrationId,
                ActivityType.TicketsChanged,
                domainEvent.ChangedAt,
                SerializeSelectionChange(domainEvent.OldTickets, domainEvent.NewTickets));
        }

        if (!SameSelection(domainEvent.OldWaitlistedTickets, domainEvent.NewWaitlistedTickets))
        {
            AddEntry(
                domainEvent.TeamId,
                domainEvent.TicketedEventId,
                domainEvent.RegistrationId,
                ActivityType.WaitlistSelectionChanged,
                domainEvent.ChangedAt,
                SerializeSelectionChange(domainEvent.OldWaitlistedTickets, domainEvent.NewWaitlistedTickets));
        }

        return ValueTask.CompletedTask;
    }

    private static bool SameSelection(
        IReadOnlyList<TicketTypeSnapshot> oldTickets, IReadOnlyList<TicketTypeSnapshot> newTickets)
        => oldTickets.Select(t => t.Id).ToHashSet().SetEquals(newTickets.Select(t => t.Id));

    private static string SerializeSelectionChange(
        IReadOnlyList<TicketTypeSnapshot> oldTickets, IReadOnlyList<TicketTypeSnapshot> newTickets)
        => JsonSerializer.Serialize(new
        {
            from = oldTickets.Select(t => t.Name.Value).ToArray(),
            to = newTickets.Select(t => t.Name.Value).ToArray()
        });

    public ValueTask HandleAsync(
        RegistrationCheckedInDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        // Only the shared-scanner source is recorded; the dashboard source is the
        // long-standing default and stays metadata-free to avoid changing existing
        // activity-log rows for signed-in crew check-ins.
        var metadata = domainEvent.Source == CheckInSource.SharedScanner
            ? JsonSerializer.Serialize(new { source = nameof(CheckInSource.SharedScanner) })
            : null;

        AddEntry(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.RegistrationId,
            ActivityType.CheckedIn,
            domainEvent.CheckedInAt,
            metadata);

        return ValueTask.CompletedTask;
    }

    public async ValueTask HandleAsync(
        WaitlistCouponIssuedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        // A coupon can be issued before the recipient has ever registered (e.g. a VIP promotion
        // straight off the waitlist), in which case there is no registration to attach the entry to.
        var registrationId = domainEvent.RegistrationId
            ?? await ResolveRegistrationIdAsync(
                domainEvent.TeamId, domainEvent.TicketedEventId, domainEvent.RecipientEmail, cancellationToken);
        if (registrationId is null)
            return;

        var metadata = JsonSerializer.Serialize(new
        {
            ticketType = domainEvent.TicketTypeName,
            expiresAt = domainEvent.ExpiresAt,
            reason = domainEvent.Reason.ToString()
        });

        AddEntry(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            registrationId.Value,
            ActivityType.WaitlistOfferSent,
            domainEvent.OccurredOn,
            metadata);
    }

    public async ValueTask HandleAsync(
        WaitlistCouponExpiredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var registrationId = await ResolveRegistrationIdAsync(
            domainEvent.TeamId, domainEvent.TicketedEventId, domainEvent.RecipientEmail, cancellationToken);
        if (registrationId is null)
            return;

        var metadata = JsonSerializer.Serialize(new { ticketType = domainEvent.TicketTypeName });

        AddEntry(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            registrationId.Value,
            ActivityType.WaitlistOfferExpired,
            domainEvent.OccurredOn,
            metadata);
    }

    public async ValueTask HandleAsync(
        WaitlistEntryRemovedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var registrationId = await ResolveRegistrationIdAsync(
            domainEvent.TeamId, domainEvent.TicketedEventId, domainEvent.Email, cancellationToken);
        if (registrationId is null)
            return;

        AddEntry(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            registrationId.Value,
            ActivityType.WaitlistRemoved,
            domainEvent.OccurredOn);
    }

    private async ValueTask<RegistrationId?> ResolveRegistrationIdAsync(
        TeamId teamId,
        TicketedEventId eventId,
        EmailAddress email,
        CancellationToken cancellationToken)
    {
        // Domain events are dispatched before SaveChanges persists anything, so a registration created earlier
        // in the same unit of work (e.g. redeeming a waitlist coupon into a brand-new registration) only exists
        // in the change tracker, not yet in the database — check there first before falling back to a query.
        var tracked = writeStore.Registrations.Local
            .FirstOrDefault(r => r.TeamId == teamId && r.EventId == eventId && r.Email == email);
        if (tracked is not null)
            return tracked.Id;

        var registration = await writeStore.Registrations
            .Where(r => r.TeamId == teamId && r.EventId == eventId && r.Email == email)
            .Select(r => new { r.Id })
            .FirstOrDefaultAsync(cancellationToken);

        return registration?.Id;
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
