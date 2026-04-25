using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.QueryRegistrations;

internal sealed class QueryRegistrationsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<QueryRegistrationsQuery, IReadOnlyList<RegistrationListItemDto>>
{
    public async ValueTask<IReadOnlyList<RegistrationListItemDto>> HandleAsync(
        QueryRegistrationsQuery query,
        CancellationToken cancellationToken)
    {
        var filter = query.Filter;

        var q = writeStore.Registrations
            .AsNoTracking()
            .Where(r => r.EventId == query.EventId);

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
            var slugList = slugs.ToArray();
            q = q.Where(r => r.Tickets.Any(t => slugList.Contains(t.Slug)));
        }

        var registrations = await q
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        if (registrations.Count == 0)
            return [];

        IEnumerable<Domain.Entities.Registration> filtered = registrations;

        if (filter.AdditionalDetailEquals is { Count: > 0 } detailFilters)
        {
            filtered = filtered.Where(r =>
                detailFilters.All(kvp =>
                    r.AdditionalDetails.TryGetValue(kvp.Key, out var v) && v == kvp.Value));
        }

        return filtered
            .Select(r => new RegistrationListItemDto(
                r.Id.Value,
                r.Email.Value,
                r.FirstName.Value,
                r.LastName.Value,
                r.Tickets.Select(t => t.Slug).ToArray(),
                r.AdditionalDetails.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                r.Status,
                r.HasReconfirmed,
                r.ReconfirmedAt))
            .ToList();
    }
}
