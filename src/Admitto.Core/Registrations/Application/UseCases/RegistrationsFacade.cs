using GetRegistrationsNs = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases;

internal sealed class RegistrationsFacade(
    IQueryHandler<GetRegistrationsNs.GetRegistrationsQuery, IReadOnlyList<GetRegistrationsNs.RegistrationListItemDto>?>
        getRegistrationsHandler,
    IRegistrationsWriteStore writeStore) : IRegistrationsFacade
{
    public async Task<IReadOnlyList<RegistrationListItemDto>> GetRegistrationsAsync(
        Guid teamId,
        Guid eventId,
        QueryRegistrationsDto query,
        CancellationToken cancellationToken = default)
    {
        var ticketedEventId = TicketedEventId.From(eventId);
        var team = TeamId.From(teamId);

        var result = await getRegistrationsHandler.HandleAsync(
            new GetRegistrationsNs.GetRegistrationsQuery(ticketedEventId, team, query),
            cancellationToken);

        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ticketedEventId && c.TeamId == team, cancellationToken);

        var maxReconfirmationEmailsByTypeId = catalog?.TicketTypes
            .Where(t => t.MaxReconfirmationEmails.HasValue)
            .ToDictionary(t => t.Id.Value, t => t.MaxReconfirmationEmails!.Value.Value)
            ?? new Dictionary<Guid, int>();

        return (result ?? [])
            .Select(r =>
            {
                var ticketTypeIds = r.Tickets.Select(t => t.Id).ToArray();
                var relevantEmailLimits = ticketTypeIds
                    .Where(id => maxReconfirmationEmailsByTypeId.ContainsKey(id))
                    .Select(id => maxReconfirmationEmailsByTypeId[id])
                    .ToList();
                var effectiveMax = relevantEmailLimits.Count > 0 ? (int?)relevantEmailLimits.Min() : null;

                return new RegistrationListItemDto(
                    r.Id,
                    r.Email,
                    r.FirstName,
                    r.LastName,
                    ticketTypeIds,
                    r.AdditionalDetails,
                    r.CreatedAt,
                    r.RegistrationCycleId,
                    r.RegistrationVersion,
                    r.TicketCatalogVersion,
                    r.Status,
                    r.HasReconfirmed,
                    r.ReconfirmedAt,
                    effectiveMax);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<AdditionalDetailFieldDto>> GetAdditionalDetailSchemaAsync(
        Guid teamId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var ticketedEventId = TicketedEventId.From(eventId);
        var team = TeamId.From(teamId);

        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ticketedEventId && e.TeamId == team, cancellationToken);

        if (ticketedEvent is null)
            return [];

        return ticketedEvent.AdditionalDetailSchema.Fields
            .Select(f => new AdditionalDetailFieldDto(f.Key, f.Name))
            .ToList();
    }
}
