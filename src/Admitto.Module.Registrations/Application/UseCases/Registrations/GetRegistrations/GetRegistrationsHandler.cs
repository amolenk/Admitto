using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrations;

internal sealed class GetRegistrationsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetRegistrationsQuery, IReadOnlyList<RegistrationListItemDto>>
{
    public async ValueTask<IReadOnlyList<RegistrationListItemDto>> HandleAsync(
        GetRegistrationsQuery query,
        CancellationToken cancellationToken)
    {
        var registrations = await writeStore.Registrations
            .Where(r => r.EventId == query.EventId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (registrations.Count == 0)
            return [];

        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.EventId, cancellationToken);

        var nameBySlug = catalog?.TicketTypes.ToDictionary(t => t.Id, t => t.Name.Value)
                         ?? new Dictionary<string, string>();

        return registrations
            .Select(r => new RegistrationListItemDto(
                r.Id.Value,
                r.Email.Value,
                r.FirstName.Value,
                r.LastName.Value,
                r.Tickets
                    .Select(t => new TicketSummaryDto(
                        t.Slug,
                        nameBySlug.TryGetValue(t.Slug, out var name) ? name : t.Slug))
                    .ToList(),
                r.CreatedAt,
                r.Status,
                r.HasReconfirmed,
                r.ReconfirmedAt))
            .ToList();
    }
}
