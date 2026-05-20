using GetRegistrationsNs = Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetActiveReconfirmTriggerSpecs;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetReconfirmTriggerSpec;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventEmailContext;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases;

internal sealed class RegistrationsFacade(
    GetTicketedEventEmailContextHandler getEmailContextHandler,
    GetRegistrationsNs.GetRegistrationsHandler getRegistrationsHandler,
    GetReconfirmTriggerSpecHandler getReconfirmTriggerSpecHandler,
    GetActiveReconfirmTriggerSpecsHandler getActiveReconfirmTriggerSpecsHandler,
    IRegistrationsWriteStore writeStore) : IRegistrationsFacade
{
    public async ValueTask<TicketedEventEmailContextDto> GetTicketedEventEmailContextAsync(
        Guid ticketedEventId,
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        return await getEmailContextHandler.HandleAsync(
            new GetTicketedEventEmailContextQuery(ticketedEventId, registrationId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<RegistrationListItemDto>> QueryRegistrationsAsync(
        TicketedEventId eventId,
        QueryRegistrationsDto query,
        CancellationToken cancellationToken = default)
    {
        var result = await getRegistrationsHandler.HandleAsync(
            new GetRegistrationsNs.GetRegistrationsQuery(eventId, Filter: query),
            cancellationToken);

        // No TeamId guard — event existence is assumed by cross-module callers.
        // Map to Contracts DTO (IDs only; names are available but not part of the contract).
        return (result ?? [])
            .Select(r => new RegistrationListItemDto(
                r.Id,
                r.Email,
                r.FirstName,
                r.LastName,
                r.Tickets.Select(t => t.Id).ToArray(),
                r.AdditionalDetails,
                r.CreatedAt,
                r.Status,
                r.HasReconfirmed,
                r.ReconfirmedAt))
            .ToList();
    }

    public async Task<ReconfirmTriggerSpecDto?> GetReconfirmTriggerSpecAsync(
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        return await getReconfirmTriggerSpecHandler.HandleAsync(
            new GetReconfirmTriggerSpecQuery(eventId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyList<ReconfirmTriggerSpecDto>> GetActiveReconfirmTriggerSpecsAsync(
        CancellationToken cancellationToken = default)
    {
        return await getActiveReconfirmTriggerSpecsHandler.HandleAsync(
            new GetActiveReconfirmTriggerSpecsQuery(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<BadgeExportRegistrationDto>> QueryRegistrationsForBadgeExportAsync(
        TicketedEventId eventId,
        IReadOnlyList<TicketTypeId> ticketTypeIds,
        CancellationToken cancellationToken = default)
    {
        var typedIds = ticketTypeIds.ToArray();

        var registrations = await writeStore.Registrations
            .AsNoTracking()
            .Where(r =>
                r.EventId == eventId &&
                r.Status == RegistrationStatus.Registered &&
                r.Tickets.Any(t => typedIds.Contains(t.Id)))
            .ToListAsync(cancellationToken);

        return registrations
            .Select(r => new BadgeExportRegistrationDto(
                r.FirstName.Value,
                r.LastName.Value,
                r.Email.Value,
                r.AdditionalDetails.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)))
            .ToList();
    }

    public async Task<IReadOnlyList<AdditionalDetailFieldDto>> GetAdditionalDetailSchemaAsync(
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (ticketedEvent is null)
            return [];

        return ticketedEvent.AdditionalDetailSchema.Fields
            .Select(f => new AdditionalDetailFieldDto(f.Key, f.Name))
            .ToList();
    }
}
