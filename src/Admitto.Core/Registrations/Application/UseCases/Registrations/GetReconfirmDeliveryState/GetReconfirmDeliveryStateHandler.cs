using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetReconfirmDeliveryState;

internal sealed class GetReconfirmDeliveryStateHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetReconfirmDeliveryStateQuery, ReconfirmDeliveryState>
{
    public async ValueTask<ReconfirmDeliveryState> HandleAsync(
        GetReconfirmDeliveryStateQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == query.EventId && e.TeamId == query.TeamId,
                cancellationToken);

        if (ticketedEvent is null || !ticketedEvent.IsActive)
            return Suppressed(ReconfirmDeliverySuppression.EventNotActive);

        var policy = ticketedEvent.ReconfirmPolicy;
        if (policy is null)
            return Suppressed(ReconfirmDeliverySuppression.PolicyDisabled);

        var now = query.DeliveryQuery.Now;
        if (now < policy.OpensAt || now >= policy.ClosesAt)
            return Suppressed(ReconfirmDeliverySuppression.OutsideWindow);

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(ticketedEvent.TimeZone.Value);
        }
        catch (TimeZoneNotFoundException)
        {
            return Suppressed(ReconfirmDeliverySuppression.InvalidTimeZone);
        }
        catch (InvalidTimeZoneException)
        {
            return Suppressed(ReconfirmDeliverySuppression.InvalidTimeZone);
        }

        if (IsQuietHours(policy.QuietHoursStart, policy.QuietHoursEnd, now, timeZone))
            return Suppressed(ReconfirmDeliverySuppression.QuietHours);

        var registration = await writeStore.Registrations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == RegistrationId.From(query.DeliveryQuery.RegistrationId)
                     && r.EventId == query.EventId
                     && r.TeamId == query.TeamId,
                cancellationToken);

        if (registration is null)
            return Suppressed(ReconfirmDeliverySuppression.RegistrationNotFound);

        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == query.EventId && c.TeamId == query.TeamId,
                cancellationToken);
        var effectiveMaximum = GetEffectiveMaximum(catalog, registration.Tickets.Select(t => t.Id.Value));
        var state = new ReconfirmDeliveryState.Allowed(
            registration.CreatedAt,
            policy.MinEmailInterval,
            effectiveMaximum,
            GetDeliveryCutoffAt(policy, now, timeZone));

        if (registration.Status != RegistrationStatus.Registered)
            return Suppressed(ReconfirmDeliverySuppression.RegistrationCancelled);

        if (registration.HasReconfirmed)
            return Suppressed(ReconfirmDeliverySuppression.RegistrationReconfirmed);

        if (registration.RegistrationCycleId != RegistrationCycleId.From(query.DeliveryQuery.RegistrationCycleId))
            return Suppressed(ReconfirmDeliverySuppression.RegistrationCycleChanged);

        if (!registration.Tickets
                .Select(t => t.Id.Value)
                .ToHashSet()
                .SetEquals(query.DeliveryQuery.ExpectedTicketTypeIds))
        {
            return Suppressed(ReconfirmDeliverySuppression.TicketSelectionChanged);
        }

        return state;
    }

    private static ReconfirmDeliveryState Suppressed(ReconfirmDeliverySuppression reason) =>
        new ReconfirmDeliveryState.Suppressed(reason);

    private static int? GetEffectiveMaximum(
        Domain.Entities.TicketCatalog? catalog,
        IEnumerable<Guid> ticketTypeIds)
    {
        if (catalog is null)
            return null;

        var limits = catalog.TicketTypes
            .Where(t => t.MaxReconfirmationEmails.HasValue && ticketTypeIds.Contains(t.Id.Value))
            .Select(t => t.MaxReconfirmationEmails!.Value.Value)
            .ToList();
        return limits.Count > 0 ? limits.Min() : null;
    }

    private static bool IsQuietHours(
        TimeOnly? start,
        TimeOnly? end,
        DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        if (!start.HasValue || !end.HasValue)
            return false;

        var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        return start.Value < end.Value
            ? localTime >= start.Value && localTime < end.Value
            : localTime >= start.Value || localTime < end.Value;
    }

    private static DateTimeOffset GetDeliveryCutoffAt(
        TicketedEventReconfirmPolicy policy,
        DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        var cutoff = policy.ClosesAt;
        if (!policy.QuietHoursStart.HasValue || !policy.QuietHoursEnd.HasValue)
            return cutoff;

        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        var start = policy.QuietHoursStart.Value;
        var candidateDate = localNow.TimeOfDay >= start.ToTimeSpan()
            ? localNow.Date.AddDays(1)
            : localNow.Date;
        var localCandidate = DateTime.SpecifyKind(candidateDate + start.ToTimeSpan(), DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(localCandidate))
            return cutoff;

        var quietHoursStart = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localCandidate, timeZone));
        return quietHoursStart < cutoff ? quietHoursStart : cutoff;
    }
}
