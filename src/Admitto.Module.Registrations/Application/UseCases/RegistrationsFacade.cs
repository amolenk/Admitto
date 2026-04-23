using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases;

internal sealed class RegistrationsFacade(IMediator mediator) : IRegistrationsFacade
{
    public async ValueTask<TicketedEventEmailContextDto> GetTicketedEventEmailContextAsync(
        Guid ticketedEventId,
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<GetTicketedEventEmailContextQuery, TicketedEventEmailContextDto>(
            new GetTicketedEventEmailContextQuery(ticketedEventId),
            cancellationToken);
    }
}
