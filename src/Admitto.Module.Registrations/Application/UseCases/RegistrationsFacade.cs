using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.QueryRegistrations;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetActiveReconfirmTriggerSpecs;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetReconfirmTriggerSpec;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases;

internal sealed class RegistrationsFacade(IMediator mediator) : IRegistrationsFacade
{
    public async ValueTask<TicketedEventEmailContextDto> GetTicketedEventEmailContextAsync(
        Guid ticketedEventId,
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<GetTicketedEventEmailContextQuery, TicketedEventEmailContextDto>(
            new GetTicketedEventEmailContextQuery(ticketedEventId, registrationId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<RegistrationListItemDto>> QueryRegistrationsAsync(
        TicketedEventId eventId,
        QueryRegistrationsDto query,
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<QueryRegistrationsQuery, IReadOnlyList<RegistrationListItemDto>>(
            new QueryRegistrationsQuery(eventId, query),
            cancellationToken);
    }

    public async Task<ReconfirmTriggerSpecDto?> GetReconfirmTriggerSpecAsync(
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<GetReconfirmTriggerSpecQuery, ReconfirmTriggerSpecDto?>(
            new GetReconfirmTriggerSpecQuery(eventId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyList<ReconfirmTriggerSpecDto>> GetActiveReconfirmTriggerSpecsAsync(
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<GetActiveReconfirmTriggerSpecsQuery, IReadOnlyList<ReconfirmTriggerSpecDto>>(
            new GetActiveReconfirmTriggerSpecsQuery(),
            cancellationToken);
    }
}
