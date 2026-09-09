using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;

internal sealed class GetRegistrationsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetRegistrationsQuery, IReadOnlyList<RegistrationListItemDto>?>
{
    public async ValueTask<IReadOnlyList<RegistrationListItemDto>?> HandleAsync(
        GetRegistrationsQuery query,
        CancellationToken cancellationToken)
    {
        var eventExists = await writeStore.TicketedEvents
            .AnyAsync(e => e.Id == query.EventId && e.TeamId == query.TeamId, cancellationToken);

        if (!eventExists)
            return null;

        var q = writeStore.Registrations
            .AsNoTracking()
            .Where(r => r.EventId == query.EventId && r.TeamId == query.TeamId);

        if (query.Filter is { } filter)
        {
            if (filter.RegistrationStatus is { } status)
                q = q.Where(r => r.Status == status);

            if (filter.HasReconfirmed is { } hasReconfirmed)
                q = q.Where(r => r.HasReconfirmed == hasReconfirmed);

            if (filter.RegisteredAfter is { } after)
                q = q.Where(r => r.CreatedAt >= after);

            if (filter.RegisteredBefore is { } before)
                q = q.Where(r => r.CreatedAt < before);

            if (filter.TicketTypeIds is { Count: > 0 } ids)
            {
                var idList = ids.Select(TicketTypeId.From).ToArray();
                q = q.Where(r => r.Tickets.Any(t => idList.Contains(t.Id)));
            }

            if (filter.RegistrationIds is { Count: > 0 } registrationIds)
            {
                var idList = registrationIds.Select(RegistrationId.From).ToArray();
                q = q.Where(r => idList.Contains(r.Id));
            }
        }

        var registrations = await q
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        if (registrations.Count == 0)
            return [];

        IEnumerable<Domain.Entities.Registration> filtered = registrations;

        if (query.Filter?.AdditionalDetailEquals is { Count: > 0 } detailFilters)
        {
            filtered = filtered.Where(r =>
                detailFilters.All(kvp =>
                    r.AdditionalDetails.TryGetValue(kvp.Key, out var v) && v == kvp.Value));
        }

        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.EventId && c.TeamId == query.TeamId, cancellationToken);

        var nameById = catalog?.TicketTypes.ToDictionary(t => t.Id.Value, t => t.Name.Value)
                         ?? new Dictionary<Guid, string>();

        return filtered
            .Select(r => new RegistrationListItemDto(
                r.Id.Value,
                r.Email.Value,
                r.FirstName.Value,
                r.LastName.Value,
                r.Tickets
                    .Select(t => new TicketSummaryDto(
                        t.Id.Value,
                        nameById.TryGetValue(t.Id.Value, out var name) ? name : t.Name.Value))
                    .ToList(),
                r.AdditionalDetails.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                r.CreatedAt,
                r.RegistrationCycleId.Value,
                r.Version,
                catalog?.Version ?? 0,
                r.Status,
                r.HasReconfirmed,
                r.ReconfirmedAt))
            .ToList();
    }
}
