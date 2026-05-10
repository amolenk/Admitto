using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.QueryRegistrations;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetActiveReconfirmTriggerSpecs;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetReconfirmTriggerSpec;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases;

internal sealed class RegistrationsFacade(
    GetTicketedEventEmailContextHandler getEmailContextHandler,
    QueryRegistrationsHandler queryRegistrationsHandler,
    GetReconfirmTriggerSpecHandler getReconfirmTriggerSpecHandler,
    GetActiveReconfirmTriggerSpecsHandler getActiveReconfirmTriggerSpecsHandler) : IRegistrationsFacade
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
        return await queryRegistrationsHandler.HandleAsync(
            new QueryRegistrationsQuery(eventId, query),
            cancellationToken);
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
}
