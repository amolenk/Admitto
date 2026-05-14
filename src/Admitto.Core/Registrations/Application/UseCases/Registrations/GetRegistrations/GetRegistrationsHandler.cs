using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;

internal sealed class GetRegistrationsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetRegistrationsQuery, IReadOnlyList<RegistrationListItemDto>?>
{
    public async ValueTask<IReadOnlyList<RegistrationListItemDto>?> HandleAsync(
        GetRegistrationsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.TeamId is { } teamId)
        {
            var eventExists = await writeStore.TicketedEvents
                .AnyAsync(e => e.Id == query.EventId && e.TeamId == teamId, cancellationToken);

            if (!eventExists)
                return null;
        }

        var q = writeStore.Registrations
            .AsNoTracking()
            .Where(r => r.EventId == query.EventId);

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

            if (filter.TicketTypeSlugs is { Count: > 0 } slugs)
            {
                var slugList = slugs.Select(Slug.From).ToArray();
                q = q.Where(r => r.Tickets.Any(t => slugList.Contains(t.Slug)));
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
            .FirstOrDefaultAsync(c => c.Id == query.EventId, cancellationToken);

        var nameBySlug = catalog?.TicketTypes.ToDictionary(t => t.Id, t => t.Name.Value)
                         ?? new Dictionary<string, string>();

        return filtered
            .Select(r => new RegistrationListItemDto(
                r.Id.Value,
                r.Email.Value,
                r.FirstName.Value,
                r.LastName.Value,
                r.Tickets
                    .Select(t => new TicketSummaryDto(
                        t.Slug.Value,
                        nameBySlug.TryGetValue(t.Slug.Value, out var name) ? name : t.Slug.Value))
                    .ToList(),
                r.AdditionalDetails.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                r.CreatedAt,
                r.Status,
                r.HasReconfirmed,
                r.ReconfirmedAt))
            .ToList();
    }
}
