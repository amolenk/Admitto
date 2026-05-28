using GetRegistrationsNs = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetActiveReconfirmTriggerSpecs;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetReconfirmTriggerSpec;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases;

internal sealed class RegistrationsFacade(
    IQueryHandler<GetTicketedEventEmailContextQuery, EventRegistrationSnapshotDto> getEmailContextHandler,
    IQueryHandler<GetRegistrationsNs.GetRegistrationsQuery, IReadOnlyList<GetRegistrationsNs.RegistrationListItemDto>?>
        getRegistrationsHandler,
    IQueryHandler<GetReconfirmTriggerSpecQuery, ReconfirmTriggerSpecDto?> getReconfirmTriggerSpecHandler,
    IQueryHandler<GetActiveReconfirmTriggerSpecsQuery, IReadOnlyList<ReconfirmTriggerSpecDto>>
        getActiveReconfirmTriggerSpecsHandler,
    IRegistrationsWriteStore writeStore) : IRegistrationsFacade
{
    public async ValueTask<EventRegistrationSnapshotDto> GetEventRegistrationSnapshotAsync(
        Guid ticketedEventId,
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        return await getEmailContextHandler.HandleAsync(
            new GetTicketedEventEmailContextQuery(ticketedEventId, registrationId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<RegistrationListItemDto>> GetRegistrationsAsync(
        Guid eventId,
        QueryRegistrationsDto query,
        CancellationToken cancellationToken = default)
    {
        var ticketedEventId = TicketedEventId.From(eventId);

        var result = await getRegistrationsHandler.HandleAsync(
            new GetRegistrationsNs.GetRegistrationsQuery(ticketedEventId, Filter: query),
            cancellationToken);

        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ticketedEventId, cancellationToken);

        var maxAttemptsByTypeId = catalog?.TicketTypes
            .Where(t => t.MaxReconfirmAttempts.HasValue)
            .ToDictionary(t => t.Id.Value, t => t.MaxReconfirmAttempts!.Value)
            ?? new Dictionary<Guid, int>();

        return (result ?? [])
            .Select(r =>
            {
                var ticketTypeIds = r.Tickets.Select(t => t.Id).ToArray();
                var relevantAttempts = ticketTypeIds
                    .Where(id => maxAttemptsByTypeId.ContainsKey(id))
                    .Select(id => maxAttemptsByTypeId[id])
                    .ToList();
                var effectiveMax = relevantAttempts.Count > 0 ? (int?)relevantAttempts.Min() : null;

                return new RegistrationListItemDto(
                    r.Id,
                    r.Email,
                    r.FirstName,
                    r.LastName,
                    ticketTypeIds,
                    r.AdditionalDetails,
                    r.CreatedAt,
                    r.Status,
                    r.HasReconfirmed,
                    r.ReconfirmedAt,
                    effectiveMax);
            })
            .ToList();
    }

    public async Task<ReconfirmTriggerSpecDto?> GetReconfirmTriggerSpecAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await getReconfirmTriggerSpecHandler.HandleAsync(
            new GetReconfirmTriggerSpecQuery(eventId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<ReconfirmTriggerSpecDto>> GetActiveReconfirmTriggerSpecsAsync(
        CancellationToken cancellationToken = default)
    {
        return await getActiveReconfirmTriggerSpecsHandler.HandleAsync(
            new GetActiveReconfirmTriggerSpecsQuery(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<AdditionalDetailFieldDto>> GetAdditionalDetailSchemaAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var ticketedEventId = TicketedEventId.From(eventId);

        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ticketedEventId, cancellationToken);

        if (ticketedEvent is null)
            return [];

        return ticketedEvent.AdditionalDetailSchema.Fields
            .Select(f => new AdditionalDetailFieldDto(f.Key, f.Name))
            .ToList();
    }
}
