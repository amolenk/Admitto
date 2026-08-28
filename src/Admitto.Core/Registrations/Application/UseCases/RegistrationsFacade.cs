using GetRegistrationsNs = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;
using GetReconfirmDeliveryStateNs = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetReconfirmDeliveryState;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases;

internal sealed class RegistrationsFacade(
    IQueryHandler<GetRegistrationsNs.GetRegistrationsQuery, IReadOnlyList<GetRegistrationsNs.RegistrationListItemDto>?>
        getRegistrationsHandler,
    IRegistrationsWriteStore writeStore,
    IQueryHandler<GetReconfirmDeliveryStateNs.GetReconfirmDeliveryStateQuery, ReconfirmDeliveryState>
        getReconfirmDeliveryStateHandler) : IRegistrationsFacade
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

        return (result ?? [])
            .Select(r =>
            {
                var ticketTypeIds = r.Tickets.Select(t => t.Id).ToArray();

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
                    GetEffectiveMaximum(catalog, ticketTypeIds));
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

    public async Task<ReconfirmDeliveryState> GetReconfirmDeliveryStateAsync(
        Guid teamId,
        Guid eventId,
        ReconfirmDeliveryQuery query,
        CancellationToken cancellationToken = default)
    {
        return await getReconfirmDeliveryStateHandler.HandleAsync(
            new GetReconfirmDeliveryStateNs.GetReconfirmDeliveryStateQuery(
                TeamId.From(teamId),
                TicketedEventId.From(eventId),
                query),
            cancellationToken);
    }

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
}
